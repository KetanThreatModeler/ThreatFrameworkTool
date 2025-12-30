using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ThreatFramework.Infra.Contract.Repository;
using ThreatModeler.TF.Core.Model.AssistRules;
using ThreatModeler.TF.Core.Model.CoreEntities;
using ThreatModeler.TF.Infra.Contract.AssistRuleIndex.Client;
using ThreatModeler.TF.Infra.Contract.AssistRuleIndex.Common.Model;
using ThreatModeler.TF.Infra.Contract.AssistRuleIndex.Common.Writer;
using ThreatModeler.TF.Infra.Contract.AssistRuleIndex.TRC;
using ThreatModeler.TF.Infra.Contract.Repository;
using ThreatModeler.TF.Infra.Implmentation.AssistRuleIndex.Common;

namespace ThreatModeler.TF.Infra.Implmentation.AssistRuleIndex.Client
{
    public sealed class ClientAssistRuleIndexManager : IClientAssistRuleIndexManager
    {
        private readonly IRepositoryHubFactory _hubFactory;
        private readonly IRepositoryHub _repositoryHub;
        private readonly ITRCAssistRuleIndexService _trcAssistRuleIndexService;
        private readonly IAssistRuleIndexSerializer _serializer;
        private readonly ITextFileStore _fileStore;
        private readonly PathOptions _pathOptions;
        private readonly ILogger<ClientAssistRuleIndexManager> _logger;

        public ClientAssistRuleIndexManager(
            IRepositoryHubFactory hubFactory,
            ITRCAssistRuleIndexService trcAssistRuleIndexService,
            IAssistRuleIndexSerializer serializer,
            ITextFileStore fileStore,
            IOptions<PathOptions> pathOptions,
            ILogger<ClientAssistRuleIndexManager> logger)
        {
            _hubFactory = hubFactory ?? throw new ArgumentNullException(nameof(hubFactory));
            _repositoryHub = _hubFactory.Create(DataPlane.Client);
            _trcAssistRuleIndexService = trcAssistRuleIndexService ?? throw new ArgumentNullException(nameof(trcAssistRuleIndexService));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _fileStore = fileStore ?? throw new ArgumentNullException(nameof(fileStore));
            _pathOptions = pathOptions?.Value ?? throw new ArgumentNullException(nameof(pathOptions));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IReadOnlyList<AssistRuleIndexEntry>> BuildAndWriteAsync(IEnumerable<Guid> libraryGuids)
        {
            var path = GetConfiguredPathOrThrow();
            var libs = NormalizeLibrariesOrThrow(libraryGuids);

            try
            {
                _logger.LogInformation("BuildAndWrite Client AssistRules index started. Path={Path}, Libraries={Count}", path, libs.Count);

                var relationships = await _repositoryHub.Relationships.GetAllRelationshipsAsync().ConfigureAwait(false);
                var rtvs = await _repositoryHub.ResourceTypeValues.GetByLibraryIdsAsync(libs).ConfigureAwait(false);

                var entries = await ComposeEntriesAsync(relationships, rtvs ?? Enumerable.Empty<ResourceTypeValues>()).ConfigureAwait(false);

                await WriteYamlAsync(path, entries).ConfigureAwait(false);
                _logger.LogInformation("Client AssistRules index YAML written. Path={Path}, Entries={Count}", path, entries.Count);

                return entries;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while building/writing Client AssistRules index YAML. Path={Path}", path);
                throw new Exception("Something went wrong while writing client assist-rules index.", ex);
            }
        }

        public async Task<IReadOnlyList<AssistRuleIndexEntry>> BuildAndWriteAsync()
        {
            var path = GetConfiguredPathOrThrow();

            try
            {
                _logger.LogInformation("BuildAndWrite Client AssistRules index (global) started. Path={Path}", path);

                var relationships = await _repositoryHub.Relationships.GetAllRelationshipsAsync().ConfigureAwait(false);
                var rtvs = await _repositoryHub.ResourceTypeValues.GetAllAsync().ConfigureAwait(false);

                var entries = await ComposeEntriesAsync(relationships, rtvs ?? Enumerable.Empty<ResourceTypeValues>()).ConfigureAwait(false);

                await WriteYamlAsync(path, entries).ConfigureAwait(false);
                _logger.LogInformation("Client AssistRules index YAML written (global). Path={Path}, Entries={Count}", path, entries.Count);

                return entries;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while building/writing Client AssistRules index YAML (global). Path={Path}", path);
                throw new Exception("Something went wrong while writing client assist-rules index.", ex);
            }
        }

        public async Task<IReadOnlyList<AssistRuleIndexEntry>> ReloadFromYamlAsync()
        {
            var path = GetConfiguredPathOrThrow();

            try
            {
                _logger.LogInformation("Reload Client AssistRules index started. Path={Path}", path);

                var yaml = await _fileStore.ReadAllTextAsync(path).ConfigureAwait(false);
                var entries = _serializer.Deserialize(yaml).ToList();

                _logger.LogInformation("Client AssistRules index reloaded. Path={Path}, Entries={Count}", path, entries.Count);
                return entries;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while reloading Client AssistRules index from YAML. Path={Path}", path);
                throw new Exception("Something went wrong while reloading client assist-rules index.", ex);
            }
        }


        private async Task<IReadOnlyList<AssistRuleIndexEntry>> ComposeEntriesAsync(
            IEnumerable<Relationship> relationships,
            IEnumerable<ResourceTypeValues> rtvs)
        {
            // Start assigning new IDs above TRC max
            var maxAssignedId = await _trcAssistRuleIndexService.GetMaxAssignedIdAsync().ConfigureAwait(false);
            var nextNewId = maxAssignedId + 1;

            var list = new List<AssistRuleIndexEntry>();

            foreach (var r in relationships ?? Enumerable.Empty<Relationship>())
            {
                var id = await _trcAssistRuleIndexService.GetIdByRelationshipGuidAsync(r.Guid).ConfigureAwait(false);
                if (id < 0) id = nextNewId++;

                list.Add(new AssistRuleIndexEntry
                {
                    Type = AssistRuleType.Relationship,
                    Identity = r.Guid.ToString(),
                    LibraryGuid = Guid.Empty,
                    Id = id
                });
            }

            foreach (var v in rtvs ?? Enumerable.Empty<ResourceTypeValues>())
            {
                var id = await _trcAssistRuleIndexService.GetIdByResourceTypeValueAsync(v.ResourceTypeValue).ConfigureAwait(false);
                if (id < 0) id = nextNewId++;

                list.Add(new AssistRuleIndexEntry
                {
                    Type = AssistRuleType.ResourceTypeValues,
                    Identity = ResourceTypeValueNormalizer.Normalize(v.ResourceTypeValue),
                    LibraryGuid = v.LibraryId,
                    Id = id
                });
            }

            return list
                .OrderBy(e => e.Type)
                .ThenBy(e => e.LibraryGuid)
                .ThenBy(e => e.Identity, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private async Task WriteYamlAsync(string path, IReadOnlyList<AssistRuleIndexEntry> entries)
        {
            var yaml = _serializer.Serialize(entries);
            await _fileStore.WriteAllTextAsync(path, yaml).ConfigureAwait(false);
        }

        private string GetConfiguredPathOrThrow()
        {
            var path = _pathOptions.AssistRuleIndexYaml;
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("AssistRuleIndexYaml is not configured in PathOptions.", nameof(_pathOptions.AssistRuleIndexYaml));
            return path;
        }

        private static List<Guid> NormalizeLibrariesOrThrow(IEnumerable<Guid> libraryGuids)
        {
            if (libraryGuids == null) throw new ArgumentNullException(nameof(libraryGuids));

            var libs = libraryGuids.Where(g => g != Guid.Empty).Distinct().ToList();
            if (libs.Count == 0)
                throw new ArgumentException("At least one non-empty library guid is required.", nameof(libraryGuids));

            return libs;
        }
    }
}
