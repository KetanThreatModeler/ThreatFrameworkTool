using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using ThreatFramework.Infra.Contract.Index;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Microsoft.Extensions.Caching.Memory;
using ThreatModeler.TF.Infra.Implmentation.Index;

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

        public async Task RefreshAsync(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path is required.", nameof(path));

            var indices = await _reader.LoadAsync(path).ConfigureAwait(false);
            var indexData = new GuidIndexData(indices);
            
            _cache.Set(MapKey, indexData);
            _cache.Set(PathKey, Path.GetFullPath(path));
            _log.LogInformation("Refreshed index cache from {Path}. Count={Count}", path, indexData.Count);
        }

        private int GetMaxId()
        {
            var maxFromCache = 0;
            if (_cache.TryGetValue(MapKey, out GuidIndexData? indexData) && indexData is not null)
            {
                maxFromCache = indexData.MaxId;
            }

            var maxFromDynamic = DynamicAssignments.Values.DefaultIfEmpty(0).Max();
            return Math.Max(maxFromCache, maxFromDynamic);
        }

        public int GetInt(Guid guid)
        {
            // 1) try current cached map
            if (_cache.TryGetValue(MapKey, out GuidIndexData? indexData) && indexData is not null)
            {
                if (indexData.TryGetId(guid, out var id))
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
                    if (!_cache.TryGetValue(MapKey, out indexData) || indexData is null)
                    {
                        var indices = _reader.LoadAsync(lastPath!).GetAwaiter().GetResult();
                        indexData = new GuidIndexData(indices);
                        _cache.Set(MapKey, indexData);
                    }
                }

                if (indexData.TryGetId(guid, out var id))
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

        public async Task GenerateAsync(string? outputPath = null)
        {
            if (string.IsNullOrWhiteSpace(outputPath))
                throw new ArgumentException("Path is required.", nameof(outputPath));

            // 1) collect entity identifiers and assign 1..N deterministically
            var entities = await _source.GetAllGuidsWithTypeAsync();
            var ordered = entities.Distinct().OrderBy(e => e.Guid).ToList();

            var guidMap = new Dictionary<Guid, int>(ordered.Count);
            var entityIndex = new List<object>(ordered.Count);
            
            int i = 1;
            foreach (var entity in ordered)
            {
                guidMap[entity.Guid] = i;
                entityIndex.Add(new
                {
                    Id = i,
                    Guid = entity.Guid.ToString(),
                    LibraryId = entity.LibraryGuid.ToString(),
                    EntityType = entity.EntityType.ToString()
                });
                i++;
            }

            // 2) create comprehensive index structure
            var indexData = new
            {
                Entities = entityIndex
            };

            // 3) serialize to YAML
            var yaml = Serializer.Serialize(indexData);

            // 4) atomic write
            var dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrWhiteSpace(dir)) Directory.CreateDirectory(dir!);
            var temp = Path.Combine(dir ?? ".", Path.GetRandomFileName());
            await File.WriteAllTextAsync(temp, yaml).ConfigureAwait(false);
            if (File.Exists(outputPath)) File.Replace(temp, outputPath, null);
            else File.Move(temp, outputPath);

            // 5) refresh current cache with proper GuidIndexData structure
            var properIndexData = new GuidIndexData(guidMap, ordered);
            _cache.Set(MapKey, properIndexData);
            _cache.Set(PathKey, Path.GetFullPath(outputPath));
            _log.LogInformation("Generated updated index at {Path}. Count={Count}", outputPath, guidMap.Count);
        }
    }
}