using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ThreatFramework.Infra.Contract;
using ThreatFramework.Infra.Contract.Index;
using ThreatModeler.TF.Core.Model.CoreEntities;
using ThreatModeler.TF.Infra.Contract.Index.TRC;
using ThreatModeler.TF.Infra.Implmentation.Index;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ThreatModeler.TF.Infra.Implmentation.Index.TRC
{
    public sealed class TRCGuidIndexService : ITRCGuidIndexService
    {
        private const string IndexKey = "GuidIndex::Entries";
        private const string PathKey = "GuidIndex::CurrentPath";

        private readonly ITRCGuidSource _source;
        private readonly IGuidIndexRepository _reader;
        private readonly IFileSystem _fs;
        private readonly IMemoryCache _cache;
        private readonly ILogger<TRCGuidIndexService> _log;
        private readonly string INDEX_PATH;

        private static readonly ConcurrentDictionary<string, SemaphoreSlim> Gates = new();

        private static readonly ISerializer Serializer =
            new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

        public TRCGuidIndexService(
            ITRCGuidSource source,
            IGuidIndexRepository reader,
            IFileSystem fs,
            IMemoryCache cache,
            IOptions<PathOptions> options,
            ILogger<TRCGuidIndexService> log)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
            _fs = fs ?? throw new ArgumentNullException(nameof(fs));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            INDEX_PATH = options?.Value.IndexYaml ?? throw new ArgumentNullException(nameof(options));

            if (string.IsNullOrWhiteSpace(INDEX_PATH))
            {
                _log.LogError("Guid index file not found at path {Path}.", INDEX_PATH);
                throw new System.IO.FileNotFoundException(
                    $"Guid index file not found at '{INDEX_PATH}'. This file is required.",
                    INDEX_PATH);
            }
            // Prime cache with configured path
            _cache.Set(PathKey, System.IO.Path.GetFullPath(INDEX_PATH));
        }

        #region Public API

        public async Task GenerateAsync()
        {
            using (_log.BeginScope("Operation: GenerateGlobalIndex Path={Path}", INDEX_PATH))
            {
                _log.LogInformation("Starting generation of global index.");

                var entities = await _source.GetAllGuidsWithTypeAsync().ConfigureAwait(false);
                await ProcessAndSaveIndexAsync(entities, INDEX_PATH).ConfigureAwait(false);

                _log.LogInformation("Global index generation completed successfully.");
            }
        }

        public async Task GenerateForLibraryAsync(IEnumerable<Guid> libIds)
        {
            if (libIds == null || !libIds.Any())
                throw new ArgumentException("Library IDs are required.", nameof(libIds));

            using (_log.BeginScope("Operation: GenerateLibraryIndex Path={Path}", INDEX_PATH))
            {
                _log.LogInformation("Starting generation of library-specific index.");

                var entities = await _source.GetGuidsWithTypeByLibraryIdsAsync(libIds).ConfigureAwait(false);
                await ProcessAndSaveIndexAsync(entities, INDEX_PATH).ConfigureAwait(false);

                _log.LogInformation("Library-specific index generation completed successfully.");
            }
        }

        public async Task RefreshAsync()
        {
            using (_log.BeginScope("Operation: RefreshIndex Path={Path}", INDEX_PATH))
            {
                var entries = (await _reader.LoadAsync(INDEX_PATH).ConfigureAwait(false)).ToList();
                CacheEntries(entries, INDEX_PATH);

                _log.LogInformation("Refreshed index cache. Loaded {Count} entities.", entries.Count);
            }
        }

        public async Task<int> GetIntAsync(Guid guid)
        {
            if (guid == Guid.Empty)
                throw new ArgumentException("Guid must be a non-empty value.", nameof(guid));

            var entries = await EnsureEntriesLoadedAsync();

            var entry = entries.FirstOrDefault(e => e.Guid == guid);
            if (entry is null)
            {
                _log.LogError("Guid {Guid} not found in TRC cached index.", guid);
                throw new KeyNotFoundException($"Guid {guid} ot found in TRC cached index.");
            }

            return entry.Id;
        }

        public async Task<Guid> GetGuidAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentOutOfRangeException(nameof(id), "Id must be a positive integer.");

            var entries = await EnsureEntriesLoadedAsync();

            var entry = entries.FirstOrDefault(e => e.Id == id);
            if (entry is null)
            {
                _log.LogError("Id {Id} not found in cached index.", id);
                throw new KeyNotFoundException($"Id {id} not found in cached index.");
            }

            return entry.Guid;
        }

        public async Task<IReadOnlyCollection<int>> GetIdsByLibraryAndTypeAsync(Guid libraryId, EntityType entityType)
        {
            if (libraryId == Guid.Empty)
                throw new ArgumentException("LibraryId must be a non-empty GUID.", nameof(libraryId));

            var entries = await EnsureEntriesLoadedAsync();

            var ids = entries
                .Where(e => e.LibraryGuid == libraryId && e.EntityType == entityType)
                .Select(e => e.Id)
                .ToList();

            if (ids.Count == 0)
            {
                _log.LogError("No IDs found for LibraryId={LibraryId}, EntityType={EntityType} in cached index.",
                    libraryId, entityType);

                throw new KeyNotFoundException(
                    $"No IDs found for LibraryId={libraryId} and EntityType={entityType} in cached index.");
            }

            return ids;
        }

        public async Task<IReadOnlyCollection<int>> GetComponentIdsAsync(Guid libraryId)
            => await GetIdsByLibraryAndTypeAsync(libraryId, EntityType.Component);

        public async Task<IReadOnlyCollection<int>> GetThreatIdsAsync(Guid libraryId)
            => await GetIdsByLibraryAndTypeAsync(libraryId, EntityType.Threat);

        public async Task<IReadOnlyCollection<int>> GetSecurityRequirementIdsAsync(Guid libraryId)
            => await GetIdsByLibraryAndTypeAsync(libraryId, EntityType.SecurityRequirement);

        public async Task<IReadOnlyCollection<int>> GetPropertyIdsAsync(Guid libraryId)
            => await GetIdsByLibraryAndTypeAsync(libraryId, EntityType.Property);

        public async Task<IReadOnlyCollection<int>> GetTestCaseIdsAsync(Guid libraryId)
            => await GetIdsByLibraryAndTypeAsync(libraryId, EntityType.TestCase);

        public async Task<(int entityId, int libId)> GetIntIdOfEntityAndLibIdByGuidAsync(Guid guid)
        {
            if (guid == Guid.Empty)
                throw new ArgumentException("Guid must be a non-empty value.", nameof(guid));

            using (_log.BeginScope("Operation: GetIntIdOfEntityAndLibIdByGuidAsync Guid={Guid}", guid))
            {
                var entries = await EnsureEntriesLoadedAsync();

                var entityEntry = entries.FirstOrDefault(e => e.Guid == guid);
                if (entityEntry is null)
                {
                    _log.LogError("Guid {Guid} not found in cached index.", guid);
                    throw new KeyNotFoundException($"Guid {guid} not found in cached index.");
                }

                var libraryEntry = entries.FirstOrDefault(e => e.Guid == entityEntry.LibraryGuid);
                if (libraryEntry is null)
                {
                    _log.LogError("LibraryGuid {LibraryGuid} not found in cached index (entity Guid={Guid}).",
                        entityEntry.LibraryGuid, guid);

                    throw new KeyNotFoundException(
                        $"LibraryGuid {entityEntry.LibraryGuid} not found in cached index (entity Guid={guid}).");
                }

                return (entityEntry.Id, libraryEntry.Id);
            }
        }

        #endregion

        #region Generation / Save

        private async Task ProcessAndSaveIndexAsync(IEnumerable<EntityIdentifier> entities, string outputPath)
        {
            if (entities is null) throw new ArgumentNullException(nameof(entities));
            if (string.IsNullOrWhiteSpace(outputPath)) throw new ArgumentException("Output path is required.", nameof(outputPath));

            var fullPath = System.IO.Path.GetFullPath(outputPath);

            var ordered = entities.Distinct().OrderBy(e => e.Guid).ToList();
            _log.LogDebug("Processing {Count} entities for indexing.", ordered.Count);

            var dto = new List<GuidIndex>(ordered.Count);
            int i = 1;

            foreach (var entity in ordered)
            {
                dto.Add(new GuidIndex
                {
                    Id = i++,
                    Guid = entity.Guid,
                    LibraryGuid = entity.LibraryGuid,
                    EntityType = entity.EntityType
                });
            }

            var yamlData = new { Entities = dto };
            var yamlContent = Serializer.Serialize(yamlData);

            await _fs.AtomicWriteAllTextAsync(fullPath, yamlContent).ConfigureAwait(false);
            CacheEntries(dto, fullPath);
            _log.LogInformation("Cache updated with {Count} entities after generation.", dto.Count);
        }

        #endregion

        #region Cache Load Helpers (Single Key)

        private void CacheEntries(List<GuidIndex> entries, string fullPath)
        {
            _cache.Set(IndexKey, entries);
            _cache.Set(PathKey, fullPath);
        }

        private async Task<List<GuidIndex>> EnsureEntriesLoadedAsync()
        {
            if (_cache.TryGetValue(IndexKey, out List<GuidIndex>? entries) && entries is not null)
                return entries;

            var gate = Gates.GetOrAdd(INDEX_PATH, _ => new SemaphoreSlim(1, 1));
            gate.Wait();

            try
            {
                if (_cache.TryGetValue(IndexKey, out entries) && entries is not null)
                    return entries;

                _log.LogInformation("Cache is empty. Loading guid index from disk (sync) at {Path}", INDEX_PATH);

                var loaded = await _reader.LoadAsync(INDEX_PATH);
                CacheEntries(loaded.ToList(), INDEX_PATH);

                return loaded.ToList();
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Failed to load guid index from disk (sync) at {Path}", INDEX_PATH);
                throw;
            }
            finally
            {
                gate.Release();
            }
        }

        public async Task<int> GetMaxAssignedIdAsync()
        {
            using (_log.BeginScope("Operation: GetMaxAssignedIdAsync Path={Path}", INDEX_PATH))
            {
                try
                {
                    var entries = await EnsureEntriesLoadedAsync().ConfigureAwait(false);

                    // If index is empty, max assigned id is 0 (caller can decide to start at 1)
                    if (entries == null || entries.Count == 0)
                    {
                        _log.LogInformation("Index cache is empty. MaxAssignedId=0.");
                        return 0;
                    }

                    // GuidIndex ids are sequential, but we compute max defensively.
                    var maxId = entries.Max(e => e.Id);

                    _log.LogDebug("Resolved MaxAssignedId={MaxId} from cached entries (Count={Count}).",
                        maxId, entries.Count);

                    return maxId;
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Failed to compute MaxAssignedId from index cache/path {Path}.", INDEX_PATH);
                    throw;
                }
            }
        }
        #endregion
    }
}
