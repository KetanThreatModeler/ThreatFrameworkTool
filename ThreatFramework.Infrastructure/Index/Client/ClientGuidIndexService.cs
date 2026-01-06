using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ThreatFramework.Infra.Contract;
using ThreatFramework.Infra.Contract.Index;
using ThreatModeler.TF.Core.Model.CoreEntities;
using ThreatModeler.TF.Infra.Contract.Index.Client;
using ThreatModeler.TF.Infra.Contract.Index.TRC;
using ThreatModeler.TF.Infra.Implmentation.Index;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ThreatModeler.TF.Infra.Implmentation.Index.Client
{
    /// <summary>
    /// Client guid index service:
    /// - GenerateAsync / GenerateForLibraryAsync build the client index from IClientGuidSource
    /// - Reuse IDs from TRC index when available; otherwise assign new IDs starting at (TRC MaxAssignedId + 1)
    /// - Persist index to PathOptions.ClientIndexYaml (atomic write via IFileSystem)
    /// - RefreshAsync loads the persisted file and updates cache
    /// - All lookup methods use cache; if cache empty, it loads from disk (same behavior as TRC)
    /// </summary>
    public sealed class ClientGuidIndexService : IClientGuidIndexService
    {
        private const string IndexKey = "ClientGuidIndex::Entries";
        private const string PathKey = "ClientGuidIndex::CurrentPath";

        private readonly IClientGuidSource _source;
        private readonly ITRCGuidIndexService _trcIndex;
        private readonly IGuidIndexRepository _reader;
        private readonly IFileSystem _fs;
        private readonly IMemoryCache _cache;
        private readonly ILogger<ClientGuidIndexService> _log;
        private readonly string INDEX_PATH;

        private static readonly ConcurrentDictionary<string, SemaphoreSlim> Gates = new();

        private static readonly ISerializer Serializer =
            new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

        public ClientGuidIndexService(
            IClientGuidSource source,
            ITRCGuidIndexService trcIndex,
            IGuidIndexRepository reader,
            IFileSystem fs,
            IMemoryCache cache,
            IOptions<PathOptions> options,
            ILogger<ClientGuidIndexService> log)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _trcIndex = trcIndex ?? throw new ArgumentNullException(nameof(trcIndex));
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
            _fs = fs ?? throw new ArgumentNullException(nameof(fs));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _log = log ?? throw new ArgumentNullException(nameof(log));

            INDEX_PATH = options?.Value.ClientIndexYaml ?? throw new ArgumentNullException(nameof(options));

            if (string.IsNullOrWhiteSpace(INDEX_PATH))
            {
                _log.LogError("Client index path is missing (PathOptions.ClientIndexYaml).");
                throw new ArgumentException(
                    "Configuration error: 'ClientIndexYaml' path is missing in PathOptions.",
                    nameof(options));
            }

            // Prime cache with configured path
            _cache.Set(PathKey, System.IO.Path.GetFullPath(INDEX_PATH));
        }

        #region Public API

        public async Task GenerateAsync()
        {
            using (_log.BeginScope("Operation: Client.GenerateGlobalIndex Path={Path}", INDEX_PATH))
            {
                _log.LogInformation("Starting generation of client index.");

                var entities = await _source.GetAllGuidsWithTypeAsyncForClient().ConfigureAwait(false);
                await ProcessAndSaveIndexAsync(entities, INDEX_PATH).ConfigureAwait(false);

                _log.LogInformation("Client index generation completed successfully.");
            }
        }

        public async Task GenerateForLibraryAsync(IEnumerable<Guid> libIds)
        {
            if (libIds == null || !libIds.Any())
                throw new ArgumentException("Library IDs are required.", nameof(libIds));

            using (_log.BeginScope("Operation: Client.GenerateLibraryIndex Path={Path}", INDEX_PATH))
            {
                _log.LogInformation("Starting generation of client library-specific index.");

                var entities = await _source
                    .GetGuidsWithTypeByLibraryIdsAsyncForClient(libIds)
                    .ConfigureAwait(false);

                await ProcessAndSaveIndexAsync(entities, INDEX_PATH).ConfigureAwait(false);

                _log.LogInformation("Client library-specific index generation completed successfully.");
            }
        }

        public async Task RefreshAsync()
        {
            using (_log.BeginScope("Operation: Client.RefreshIndex Path={Path}", INDEX_PATH))
            {
                EnsureIndexFileExistsOrThrow(INDEX_PATH);

                var entries = (await _reader.LoadAsync(INDEX_PATH).ConfigureAwait(false)).ToList();
                CacheEntries(entries, System.IO.Path.GetFullPath(INDEX_PATH));

                _log.LogInformation("Refreshed client index cache. Loaded {Count} entities.", entries.Count);
            }
        }

        public async Task<int> GetIntAsync(Guid guid)
        {
            if (guid == Guid.Empty)
                throw new ArgumentException("Guid must be a non-empty value.", nameof(guid));

            var entries = await EnsureEntriesLoadedAsync().ConfigureAwait(false);

            var entry = entries.FirstOrDefault(e => e.Guid == guid);
            if (entry is null)
            {
                _log.LogError("Guid {Guid} not found in cached client index.", guid);
                throw new KeyNotFoundException($"Guid {guid} not found in cached client index.");
            }

            return entry.Id;
        }

        public async Task<Guid> GetGuidAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentOutOfRangeException(nameof(id), "Id must be a positive integer.");

            var entries = await EnsureEntriesLoadedAsync().ConfigureAwait(false);

            var entry = entries.FirstOrDefault(e => e.Id == id);
            if (entry is null)
            {
                _log.LogError("Id {Id} not found in cached client index.", id);
                throw new KeyNotFoundException($"Id {id} not found in cached client index.");
            }

            return entry.Guid;
        }

        public async Task<IReadOnlyCollection<int>> GetIdsByLibraryAndTypeAsync(Guid libraryId, EntityType entityType)
        {
            if (libraryId == Guid.Empty)
                throw new ArgumentException("LibraryId must be a non-empty GUID.", nameof(libraryId));

            var entries = await EnsureEntriesLoadedAsync().ConfigureAwait(false);

            var ids = entries
                .Where(e => e.LibraryGuid == libraryId && e.EntityType == entityType)
                .Select(e => e.Id)
                .ToList();

            if (ids.Count == 0)
            {
                _log.LogError(
                    "No IDs found for LibraryId={LibraryId}, EntityType={EntityType} in cached client index.",
                    libraryId, entityType);

                throw new KeyNotFoundException(
                    $"No IDs found for LibraryId={libraryId} and EntityType={entityType} in cached client index.");
            }

            return ids;
        }

        public Task<IReadOnlyCollection<int>> GetComponentIdsAsync(Guid libraryId)
            => GetIdsByLibraryAndTypeAsync(libraryId, EntityType.Component);

        public Task<IReadOnlyCollection<int>> GetThreatIdsAsync(Guid libraryId)
            => GetIdsByLibraryAndTypeAsync(libraryId, EntityType.Threat);

        public Task<IReadOnlyCollection<int>> GetSecurityRequirementIdsAsync(Guid libraryId)
            => GetIdsByLibraryAndTypeAsync(libraryId, EntityType.SecurityRequirement);

        public Task<IReadOnlyCollection<int>> GetPropertyIdsAsync(Guid libraryId)
            => GetIdsByLibraryAndTypeAsync(libraryId, EntityType.Property);

        public Task<IReadOnlyCollection<int>> GetTestCaseIdsAsync(Guid libraryId)
            => GetIdsByLibraryAndTypeAsync(libraryId, EntityType.TestCase);

        public async Task<(int entityId, int libId)> GetIntIdOfEntityAndLibIdByGuidAsync(Guid guid)
        {
            if (guid == Guid.Empty)
                throw new ArgumentException("Guid must be a non-empty value.", nameof(guid));

            using (_log.BeginScope("Operation: Client.GetIntIdOfEntityAndLibIdByGuidAsync Guid={Guid}", guid))
            {
                var entries = await EnsureEntriesLoadedAsync().ConfigureAwait(false);

                var entityEntry = entries.FirstOrDefault(e => e.Guid == guid);
                if (entityEntry is null)
                {
                    _log.LogError("Guid {Guid} not found in cached client index.", guid);
                    throw new KeyNotFoundException($"Guid {guid} not found in cached client index.");
                }

                var libraryEntry = entries.FirstOrDefault(e => e.Guid == entityEntry.LibraryGuid);
                if (libraryEntry is null)
                {
                    _log.LogError(
                        "LibraryGuid {LibraryGuid} not found in cached client index (entity Guid={Guid}).",
                        entityEntry.LibraryGuid, guid);

                    throw new KeyNotFoundException(
                        $"LibraryGuid {entityEntry.LibraryGuid} not found in cached client index (entity Guid={guid}).");
                }

                return (entityEntry.Id, libraryEntry.Id);
            }
        }

        #endregion

        #region Generation / Save (PERSIST + CACHE)

        private async Task ProcessAndSaveIndexAsync(IEnumerable<EntityIdentifier> entities, string outputPath)
        {
            if (entities is null) throw new ArgumentNullException(nameof(entities));
            if (string.IsNullOrWhiteSpace(outputPath)) throw new ArgumentException("Output path is required.", nameof(outputPath));

            // Ensure TRC is ready for id reuse + max seed
            await _trcIndex.RefreshAsync().ConfigureAwait(false);

            var fullPath = System.IO.Path.GetFullPath(outputPath);

            // Deterministic ordering
            var ordered = entities.Distinct().OrderBy(e => e.Guid).ToList();
            _log.LogDebug("Processing {Count} client entities for indexing.", ordered.Count);

            // Seed new assignment ids from TRC max + 1
            var maxTrcId = await _trcIndex.GetMaxAssignedIdAsync().ConfigureAwait(false);
            var nextId = maxTrcId + 1;

            var dto = new List<GuidIndex>(ordered.Count);

            int reused = 0;
            int created = 0;

            foreach (var entity in ordered)
            {
                if (entity.Guid == Guid.Empty)
                    continue;

                var trcId = await _trcIndex.GetIntForClientIndexGenerationAsync(entity.Guid).ConfigureAwait(false);
                if(trcId > 0)
                {
                    dto.Add(new GuidIndex
                    {
                        Id = trcId,
                        Guid = entity.Guid,
                        LibraryGuid = entity.LibraryGuid,
                        EntityType = entity.EntityType
                    });
                    reused++;
                }
                else
                {
                    dto.Add(new GuidIndex
                    {
                        Id = nextId++,
                        Guid = entity.Guid,
                        LibraryGuid = entity.LibraryGuid,
                        EntityType = entity.EntityType
                    });
                    created++;
                }
            }

            // Persist to yaml (same schema as TRC: root "entities")
            var yamlData = new { Entities = dto };
            var yamlContent = Serializer.Serialize(yamlData);

            await _fs.AtomicWriteAllTextAsync(fullPath, yamlContent).ConfigureAwait(false);

            // Cache
            CacheEntries(dto, fullPath);

            _log.LogInformation(
                "Client index persisted + cached. Total={Total}, ReusedFromTRC={Reused}, NewlyAssigned={Created}, NextId={NextId}, Path={Path}.",
                dto.Count, reused, created, nextId, fullPath);
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
            await gate.WaitAsync().ConfigureAwait(false);

            try
            {
                if (_cache.TryGetValue(IndexKey, out entries) && entries is not null)
                    return entries;

                _log.LogInformation("Client cache is empty. Loading client guid index from disk at {Path}", INDEX_PATH);

                EnsureIndexFileExistsOrThrow(INDEX_PATH);

                var loaded = await _reader.LoadAsync(INDEX_PATH).ConfigureAwait(false);
                CacheEntries(loaded.ToList(), System.IO.Path.GetFullPath(INDEX_PATH));

                return loaded.ToList();
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Failed to load client guid index from disk at {Path}", INDEX_PATH);
                throw;
            }
            finally
            {
                gate.Release();
            }
        }

        private void EnsureIndexFileExistsOrThrow(string path)
        {
            if (!_fs.FileExists(path))
            {
                _log.LogError("Client guid index file not found at path {Path}.", path);
                throw new System.IO.FileNotFoundException(
                    $"Client guid index file not found at '{path}'. This file is required.",
                    path);
            }
        }

        public async Task<EntityIdentifier> GetIdentifierByIdAsync(int id)
        {
            var entries = await EnsureEntriesLoadedAsync();

            var entry = entries.FirstOrDefault(e => e.Id == id);
            if (entry is null)
            {
                _log.LogWarning("Id {Id} not found in cached index of Client.", id);
                return null;
            }

            return entry;
        }

        public async Task<int> GetIntAsyncWithoutThrowingErrorAsync(Guid guid)
        {
            if (guid == Guid.Empty)
                throw new ArgumentException("Guid must be a non-empty value.", nameof(guid));

            var entries = await EnsureEntriesLoadedAsync().ConfigureAwait(false);

            var entry = entries.FirstOrDefault(e => e.Guid == guid);
            if (entry is null)
            {
                return 0;
            }

            return entry.Id;
        }

        #endregion
    }
}
