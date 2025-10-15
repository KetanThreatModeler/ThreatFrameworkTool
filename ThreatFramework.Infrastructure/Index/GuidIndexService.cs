using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using ThreatFramework.Infra.Contract.Index;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Microsoft.Extensions.Caching.Memory;

namespace ThreatFramework.Infrastructure.Index
{
    public sealed class GuidIndexService : IGuidIndexService
    {
        private const string MapKey = "GuidIndex::CurrentMap";
        private const string PathKey = "GuidIndex::CurrentPath";

        private readonly IGuidSource _source;                 // scoped, aggregates repos
        private readonly IGuidIndexRepository _reader;        // singleton OK
        private readonly IMemoryCache _cache;                 // singleton
        private readonly ILogger<GuidIndexService> _log;

        // prevent concurrent lazy-loads when cache is empty
        private static readonly ConcurrentDictionary<string, object> Gates = new();
        
        // maintain dynamically assigned GUIDs for application lifetime
        private static readonly ConcurrentDictionary<Guid, int> DynamicAssignments = new();

        private static readonly ISerializer Serializer =
            new SerializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();

        public GuidIndexService(IGuidSource source,
                                IGuidIndexRepository reader,
                                IMemoryCache cache,
                                ILogger<GuidIndexService> log)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public async Task GenerateAsync(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path is required.", nameof(path));

            // 1) collect GUIDs and assign 1..N deterministically
            var guids = await _source.GetAllGuidsAsync();
            var ordered = guids.Distinct().OrderBy(g => g).ToList();

            var map = new Dictionary<Guid, int>(ordered.Count);
            int i = 1; foreach (var g in ordered) map[g] = i++;

            // 2) serialize minimal YAML (guid: int)
            var yamlDict = map.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value);
            var yaml = Serializer.Serialize(yamlDict);

            // 3) atomic write
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(dir)) Directory.CreateDirectory(dir!);
            var temp = Path.Combine(dir ?? ".", Path.GetRandomFileName());
            await File.WriteAllTextAsync(temp, yaml).ConfigureAwait(false);
            if (File.Exists(path)) File.Replace(temp, path, null);
            else File.Move(temp, path);

            // 4) refresh current cache
            _cache.Set(MapKey, new Dictionary<Guid, int>(map));
            _cache.Set(PathKey, Path.GetFullPath(path));
            _log.LogInformation("Generated index at {Path}. Count={Count}", path, map.Count);
        }

        public async Task RefreshAsync(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path is required.", nameof(path));

            var map = await _reader.LoadAsync(path).ConfigureAwait(false);
            _cache.Set(MapKey, new Dictionary<Guid, int>(map));
            _cache.Set(PathKey, Path.GetFullPath(path));
            _log.LogInformation("Refreshed index cache from {Path}. Count={Count}", path, map.Count);
        }

        private int GetMaxId()
        {
            var maxFromCache = 0;
            if (_cache.TryGetValue(MapKey, out Dictionary<Guid, int>? map) && map is { Count: > 0 })
            {
                maxFromCache = map.Values.DefaultIfEmpty(0).Max();
            }

            var maxFromDynamic = DynamicAssignments.Values.DefaultIfEmpty(0).Max();
            return Math.Max(maxFromCache, maxFromDynamic);
        }

        public int GetInt(Guid guid)
        {
            // 1) try current cached map
            if (_cache.TryGetValue(MapKey, out Dictionary<Guid, int>? map) && map is { Count: > 0 })
            {
                if (map.TryGetValue(guid, out var id))
                    return id;
            }

            // 2) try dynamic assignments
            if (DynamicAssignments.TryGetValue(guid, out var dynamicId))
                return dynamicId;

            // 3) lazy-load from last known path if we have it
            if (_cache.TryGetValue(PathKey, out string? lastPath) && !string.IsNullOrWhiteSpace(lastPath))
            {
                var gate = Gates.GetOrAdd(lastPath, _ => new object());
                lock (gate)
                {
                    // double-check after acquiring the gate
                    if (!_cache.TryGetValue(MapKey, out map) || map is not { Count: > 0 })
                    {
                        map = (Dictionary<Guid, int>)_reader.LoadAsync(lastPath!).GetAwaiter().GetResult();
                        _cache.Set(MapKey, map);
                    }
                }

                if (map.TryGetValue(guid, out var id))
                    return id;
            
                // check dynamic assignments again after loading
                if (DynamicAssignments.TryGetValue(guid, out dynamicId))
                    return dynamicId;
            }

            // 4) not found anywhere - assign new incremental ID
            var newId = GetMaxId() + 1;
            var assignedId = DynamicAssignments.GetOrAdd(guid, newId);
            
            _log.LogInformation("Assigned new dynamic ID {Id} to GUID {Guid}", assignedId, guid);
            return assignedId;
        }
    }
}