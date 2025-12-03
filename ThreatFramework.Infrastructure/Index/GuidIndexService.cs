using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ThreatFramework.Infra.Contract.Index;
using ThreatModeler.TF.Core.Config;
using ThreatModeler.TF.Infra.Implmentation.Index;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

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
        private readonly PathOptions _pathOptions;

        // Synchronization objects
        private static readonly ConcurrentDictionary<string, object> Gates = new();
        private static readonly ConcurrentDictionary<Guid, int> DynamicAssignments = new();

        private static readonly ISerializer Serializer =
            new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

        public GuidIndexService(
            IGuidSource source,
            IGuidIndexRepository reader,
            IMemoryCache cache,
            IOptions<PathOptions> options,
            ILogger<GuidIndexService> log)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _pathOptions = options?.Value ?? throw new ArgumentNullException(nameof(options));

            if (string.IsNullOrWhiteSpace(_pathOptions.IndexYaml))
            {
                throw new ArgumentException(
                    "Configuration error: 'IndexYaml' path is missing in PathOptions.",
                    nameof(options));
            }

            // Prime cache with configured path so we always have a known location
            var indexPath = Path.GetFullPath(_pathOptions.IndexYaml);
            _cache.Set(PathKey, indexPath);
        }

        #region Public API

        public async Task GenerateAsync(string? outputPath = null)
        {
            var targetPath = ResolveIndexPathOrThrow(outputPath);

            using (_log.BeginScope("Operation: GenerateGlobalIndex Path={Path}", targetPath))
            {
                _log.LogInformation("Starting generation of global index.");

                var entities = await _source
                    .GetAllGuidsWithTypeAsync()
                    .ConfigureAwait(false);

                // Master index updates the runtime cache
                await ProcessAndSaveIndexAsync(entities, targetPath, updateCache: true)
                    .ConfigureAwait(false);

                _log.LogInformation("Global index generation completed successfully.");
            }
        }

        public async Task GenerateForLibraryAsync(IEnumerable<Guid> libIds, string? outputPath = null)
        {
            if (libIds == null || !libIds.Any())
                throw new ArgumentException("Library IDs are required.", nameof(libIds));

            var targetPath = ResolveIndexPathOrThrow(outputPath);

            using (_log.BeginScope("Operation: GenerateLibraryIndex Path={Path}", targetPath))
            {
                _log.LogInformation("Starting generation of library-specific index.");

                var entities = await _source
                    .GetGuidsWithTypeByLibraryIdsAsync(libIds)
                    .ConfigureAwait(false);

                await ProcessAndSaveIndexAsync(entities, targetPath, updateCache: true)
                    .ConfigureAwait(false);

                _log.LogInformation("Library-specific index generation completed successfully.");
            }
        }

        public async Task RefreshAsync(string? path = null)
        {
            var indexPath = ResolveIndexPathOrThrow(path);
            EnsureIndexFileExistsOrThrow(indexPath);

            using (_log.BeginScope("Operation: RefreshIndex Path={Path}", indexPath))
            {
                var indices = await _reader
                    .LoadAsync(indexPath)
                    .ConfigureAwait(false);

                var indexData = new GuidIndexData(indices.ToList());

                _cache.Set(MapKey, indexData);
                _cache.Set(PathKey, indexPath);

                _log.LogInformation("Refreshed index cache. Loaded {Count} entities.", indexData.Count);
            }
        }

        public int GetInt(Guid guid)
        {
            var indexData = EnsureIndexLoaded();

            // 1) Static index
            if (indexData.TryGetId(guid, out var id))
            {
                return id;
            }

            // 2) Dynamic assignments (in-memory)
            if (DynamicAssignments.TryGetValue(guid, out var dynamicId))
            {
                return dynamicId;
            }

            // 3) Not found – assign dynamic ID
            return AssignDynamicId(guid);
        }

        public Guid GetGuid(int id)
        {
            if (id <= 0)
                throw new ArgumentOutOfRangeException(nameof(id), "Id must be a positive integer.");

            using (_log.BeginScope("Operation: GetGuid Id={Id}", id))
            {
                var indexData = EnsureIndexLoaded();

                // 1) Static index
                if (indexData.TryGetGuid(id, out var guidFromIndex))
                {
                    _log.LogDebug("Resolved Guid {Guid} for Id={Id} from static index.", guidFromIndex, id);
                    return guidFromIndex;
                }

                // 2) Dynamic assignments (reverse lookup)
                var dynamicGuid = DynamicAssignments
                    .FirstOrDefault(kvp => kvp.Value == id)
                    .Key;

                if (dynamicGuid != Guid.Empty)
                {
                    _log.LogDebug("Resolved Guid {Guid} for Id={Id} from dynamic assignments.", dynamicGuid, id);
                    return dynamicGuid;
                }

                // 3) Not found anywhere – this is considered an error
                _log.LogWarning("No Guid found for Id={Id} in index or dynamic assignments.", id);
                throw new KeyNotFoundException($"No Guid found for Id={id}.");
            }
        }

        public IReadOnlyCollection<int> GetIdsByLibraryAndType(Guid libraryId, EntityType entityType)
        {
            if (libraryId == Guid.Empty)
                throw new ArgumentException("LibraryId must be a non-empty GUID.", nameof(libraryId));

            using (_log.BeginScope("Operation: GetIdsByLibraryAndType LibraryId={LibraryId} EntityType={EntityType}",
                                   libraryId, entityType))
            {
                try
                {
                    var indexData = EnsureIndexLoaded();

                    var ids = indexData.GetIdsForLibraryAndType(libraryId, entityType);

                    _log.LogDebug("Resolved {Count} IDs for LibraryId={LibraryId}, EntityType={EntityType}.",
                                  ids.Count, libraryId, entityType);

                    return ids;
                }
                catch (Exception ex)
                {
                    _log.LogError(ex,
                        "Failed to resolve IDs for LibraryId={LibraryId}, EntityType={EntityType}.",
                        libraryId, entityType);

                    throw;
                }
            }
        }

        // Convenience wrappers
        public IReadOnlyCollection<int> GetComponentIds(Guid libraryId)
            => GetIdsByLibraryAndType(libraryId, EntityType.Component);

        public IReadOnlyCollection<int> GetThreatIds(Guid libraryId)
            => GetIdsByLibraryAndType(libraryId, EntityType.Threat);

        public IReadOnlyCollection<int> GetSecurityRequirementIds(Guid libraryId)
            => GetIdsByLibraryAndType(libraryId, EntityType.SecurityRequirement);

        public IReadOnlyCollection<int> GetPropertyIds(Guid libraryId)
            => GetIdsByLibraryAndType(libraryId, EntityType.Property);

        public IReadOnlyCollection<int> GetTestCaseIds(Guid libraryId)
            => GetIdsByLibraryAndType(libraryId, EntityType.TestCase);

        #endregion

        #region Core Index Generation

        /// <summary>
        /// Sorts entities, assigns IDs, serializes to YAML, writes to disk, and optionally updates MemoryCache.
        /// </summary>
        private async Task ProcessAndSaveIndexAsync(IEnumerable<EntityIdentifier> entities, string outputPath, bool updateCache)
        {
            if (entities is null) throw new ArgumentNullException(nameof(entities));
            if (string.IsNullOrWhiteSpace(outputPath)) throw new ArgumentException("Output path is required.", nameof(outputPath));

            var fullPath = Path.GetFullPath(outputPath);

            // 1. Deterministic ordering
            var ordered = entities.Distinct().OrderBy(e => e.Guid).ToList();
            _log.LogDebug("Processing {Count} entities for indexing.", ordered.Count);

            // 2. Map construction (Guid -> Int) & DTO creation
            var entityIndexDto = new List<GuidIndex>(ordered.Count);

            int i = 1;
            foreach (var entity in ordered)
            {
                entityIndexDto.Add(new GuidIndex
                {
                    Id = i,
                    Guid = entity.Guid,
                    LibraryGuid = entity.LibraryGuid,
                    EntityType = entity.EntityType
                });

                i++;
            }

            // 3. Serialize – produces "entities" root (camelCase)
            var yamlData = new { Entities = entityIndexDto };
            var yamlContent = Serializer.Serialize(yamlData);

            // 4. Atomic write
            await AtomicWriteAsync(fullPath, yamlContent).ConfigureAwait(false);

            // 5. Update cache (only if requested)
            if (updateCache)
            {
                var indexData = new GuidIndexData(entityIndexDto);
                _cache.Set(MapKey, indexData);
                _cache.Set(PathKey, fullPath);

                _log.LogInformation("Global InMemory Cache updated with {Count} entities.", indexData.Count);
            }
        }

        private async Task AtomicWriteAsync(string outputPath, string content)
        {
            var dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var tempPath = Path.Combine(
                dir ?? ".",
                $"{Path.GetFileName(outputPath)}.tmp_{Guid.NewGuid()}");

            try
            {
                await File.WriteAllTextAsync(tempPath, content).ConfigureAwait(false);

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

                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }

                throw;
            }
        }

        #endregion

        #region Lookup / Dynamic IDs

        private int AssignDynamicId(Guid guid)
        {
            var nextId = GetMaxId() + 1;

            var assignedId = DynamicAssignments.GetOrAdd(guid, nextId);

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

        #endregion

        #region Index Loading / Path Helpers

        private string ResolveIndexPathOrThrow(string? explicitPath)
        {
            string? path = explicitPath;

            if (string.IsNullOrWhiteSpace(path))
            {
                if (_cache.TryGetValue(PathKey, out string? cachedPath) &&
                    !string.IsNullOrWhiteSpace(cachedPath))
                {
                    path = cachedPath;
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(_pathOptions.IndexYaml))
                    {
                        _log.LogError("Guid index path is missing from configuration.");
                        throw new InvalidOperationException("Guid index path is not configured.");
                    }

                    path = _pathOptions.IndexYaml;
                }
            }

            var fullPath = Path.GetFullPath(path);
            _cache.Set(PathKey, fullPath);

            return fullPath;
        }

        private void EnsureIndexFileExistsOrThrow(string path)
        {
            if (!File.Exists(path))
            {
                _log.LogError("Guid index file not found at path {Path}. This file is required for mapping.", path);
                throw new FileNotFoundException(
                    $"Guid index file not found at '{path}'. This file is required.",
                    path);
            }
        }

        private GuidIndexData EnsureIndexLoaded()
        {
            if (_cache.TryGetValue(MapKey, out GuidIndexData? indexData) && indexData is not null)
            {
                return indexData;
            }

            var path = ResolveIndexPathOrThrow(null);
            EnsureIndexFileExistsOrThrow(path);

            var gate = Gates.GetOrAdd(path, _ => new object());
            lock (gate)
            {
                if (_cache.TryGetValue(MapKey, out indexData) && indexData is not null)
                {
                    return indexData;
                }

                _log.LogInformation("Loading guid index from {Path}", path);

                try
                {
                    var indices = _reader.LoadAsync(path).GetAwaiter().GetResult();
                    indexData = new GuidIndexData(indices.ToList());

                    _cache.Set(MapKey, indexData);
                    _cache.Set(PathKey, path);

                    return indexData;
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Failed to load guid index from {Path}", path);
                    throw;
                }
            }
        }
        #endregion
    }
}
