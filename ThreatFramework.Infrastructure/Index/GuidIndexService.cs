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

        private readonly IGuidSource _source;
        private readonly IGuidIndexRepository _reader;
        private readonly IMemoryCache _cache;
        private readonly ILogger<GuidIndexService> _log;

        // Synchronization objects
        private static readonly ConcurrentDictionary<string, object> Gates = new();
        private static readonly ConcurrentDictionary<Guid, int> DynamicAssignments = new();

        private static readonly ISerializer Serializer =
            new SerializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();

        public GuidIndexService(
            IGuidSource source,
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
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Path is required.", nameof(path));

            using (_log.BeginScope("Operation: RefreshIndex Path={Path}", path))
            {
                var indices = await _reader.LoadAsync(path).ConfigureAwait(false);
                var indexData = new GuidIndexData(indices);

                _cache.Set(MapKey, indexData);
                _cache.Set(PathKey, Path.GetFullPath(path));

                _log.LogInformation("Refreshed index cache. Loaded {Count} entities.", indexData.Count);
            }
        }

        public int GetInt(Guid guid)
        {
            // 1) Try current cached map
            if (_cache.TryGetValue(MapKey, out GuidIndexData? indexData) && indexData is not null)
            {
                if (indexData.TryGetId(guid, out var id)) return id;
            }

            // 2) Try dynamic assignments (InMemory)
            if (DynamicAssignments.TryGetValue(guid, out var dynamicId)) return dynamicId;

            // 3) Lazy-load from last known path if cache miss
            if (_cache.TryGetValue(PathKey, out string? lastPath) && !string.IsNullOrWhiteSpace(lastPath))
            {
                return LazyLoadAndLookup(guid, lastPath!, ref indexData);
            }

            // 4) Not found anywhere - assign new incremental ID
            return AssignDynamicId(guid);
        }

        public async Task GenerateAsync(string? outputPath = null)
        {
            if (string.IsNullOrWhiteSpace(outputPath)) throw new ArgumentException("Path is required.", nameof(outputPath));

            using (_log.BeginScope("Operation: GenerateGlobalIndex Path={Path}", outputPath))
            {
                _log.LogInformation("Starting generation of global index.");

                // 1. Fetch Data
                var entities = await _source.GetAllGuidsWithTypeAsync();

                // 2. Process, Save to File, and Update Global Cache
                // We pass 'updateCache: true' because this is the Master Index
                await ProcessAndSaveIndexAsync(entities, outputPath!, updateCache: true);

                _log.LogInformation("Global index generation completed successfully.");
            }
        }

        public async Task GenerateForLibraryAsync(IEnumerable<Guid> libIds, string? outputPath = null)
        {
            if (string.IsNullOrWhiteSpace(outputPath)) throw new ArgumentException("Path is required.", nameof(outputPath));
            if (libIds == null || !libIds.Any()) throw new ArgumentException("Library IDs are required.", nameof(libIds));

            using (_log.BeginScope("Operation: GenerateLibraryIndex Path={Path}", outputPath))
            {
                _log.LogInformation("Starting generation of library-specific index.");

                // 1. Fetch Data (Filtered)
                var entities = await _source.GetGuidsWithTypeByLibraryIdsAsync(libIds);

                // 2. Process and Save to File
                // We pass 'updateCache: false'. 
                // REASON: A partial library index should not replace the Global Runtime Cache, 
                // otherwise the app would crash when trying to access entities from other libraries.
                await ProcessAndSaveIndexAsync(entities, outputPath!, updateCache: false);

                _log.LogInformation("Library-specific index generation completed successfully.");
            }
        }


        /// <summary>
        /// Core logic: Sorts entities, assigns IDs, Serializes to YAML, Writes to Disk, and optionally updates MemoryCache.
        /// </summary>
        private async Task ProcessAndSaveIndexAsync(IEnumerable<EntityIdentifier> entities, string outputPath, bool updateCache)
        {
            // 1. Deterministic Ordering
            var ordered = entities.Distinct().OrderBy(e => e.Guid).ToList();
            _log.LogDebug("Processing {Count} entities for indexing.", ordered.Count);

            // 2. Map Construction (Guid -> Int) & DTO Creation
            var guidMap = new Dictionary<Guid, int>(ordered.Count);
            var entityIndexDto = new List<object>(ordered.Count);

            int i = 1;
            foreach (var entity in ordered)
            {
                guidMap[entity.Guid] = i;
                entityIndexDto.Add(new
                {
                    Id = i,
                    Guid = entity.Guid.ToString(),
                    LibraryId = entity.LibraryGuid.ToString(),
                    EntityType = entity.EntityType.ToString()
                });
                i++;
            }

            // 3. Serialize
            var yamlData = new { Entities = entityIndexDto };
            var yamlContent = Serializer.Serialize(yamlData);

            // 4. Atomic Write
            await AtomicWriteAsync(outputPath, yamlContent);

            // 5. Update Cache (Only if requested)
            if (updateCache)
            {
                var properIndexData = new GuidIndexData(guidMap, ordered);
                _cache.Set(MapKey, properIndexData);
                _cache.Set(PathKey, Path.GetFullPath(outputPath));
                _log.LogInformation("Global InMemory Cache updated.");
            }
        }

        private async Task AtomicWriteAsync(string outputPath, string content)
        {
            var dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var tempPath = Path.Combine(dir ?? ".", $"{Path.GetFileName(outputPath)}.tmp_{Guid.NewGuid()}");

            try
            {
                await File.WriteAllTextAsync(tempPath, content).ConfigureAwait(false);

                // Atomic Swap
                if (File.Exists(outputPath))
                {
                    File.Replace(tempPath, outputPath, null);
                }
                else
                {
                    File.Move(tempPath, outputPath);
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Failed to write index file to {Path}", outputPath);
                // Ensure temp file cleanup on failure
                if (File.Exists(tempPath)) File.Delete(tempPath);
                throw;
            }
        }

        private int LazyLoadAndLookup(Guid guid, string lastPath, ref GuidIndexData? indexData)
        {
            var gate = Gates.GetOrAdd(lastPath, _ => new object());
            lock (gate)
            {
                // Double-check locking pattern
                if (!_cache.TryGetValue(MapKey, out indexData) || indexData is null)
                {
                    _log.LogInformation("Lazy-loading index from {Path}", lastPath);
                    var indices = _reader.LoadAsync(lastPath).GetAwaiter().GetResult();
                    indexData = new GuidIndexData(indices);
                    _cache.Set(MapKey, indexData);
                }
            }

            if (indexData.TryGetId(guid, out var id)) return id;

            // Check dynamic assignments again after loading
            if (DynamicAssignments.TryGetValue(guid, out var dynamicId)) return dynamicId;

            // Still not found
            return AssignDynamicId(guid);
        }

        private int AssignDynamicId(Guid guid)
        {
            // Determine the next available ID
            int nextId = GetMaxId() + 1;

            var assignedId = DynamicAssignments.GetOrAdd(guid, nextId);

            // Only log if we actually added a new one (not a race condition result)
            if (assignedId == nextId)
            {
                _log.LogWarning("Guid {Guid} not found in index. Assigned dynamic ID {Id}.", guid, assignedId);
            }

            return assignedId;
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
    }
}