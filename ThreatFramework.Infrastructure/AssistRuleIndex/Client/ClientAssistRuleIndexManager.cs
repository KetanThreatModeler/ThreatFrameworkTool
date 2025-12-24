using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThreatModeler.TF.Core.Model.AssistRules;
using ThreatModeler.TF.Core.Model.CoreEntities;
using ThreatModeler.TF.Infra.Contract.AssistRuleIndex.Builder;
using ThreatModeler.TF.Infra.Contract.AssistRuleIndex.Client;
using ThreatModeler.TF.Infra.Contract.AssistRuleIndex.Model;
using ThreatModeler.TF.Infra.Contract.AssistRuleIndex.TRC;
using ThreatModeler.TF.Infra.Contract.Repository.AssistRules;

namespace ThreatModeler.TF.Infra.Implmentation.AssistRuleIndex.Client
{
    public sealed class ClientAssistRuleIndexManager : IClientAssistRuleIndexManager
    {
        private readonly IRelationshipRepository _relationshipRepository;
        private readonly IResourceTypeValuesRepository _resourceTypeValuesRepository;
        private readonly ITRCAssistRuleIndexService _trcAssistRuleIndexService;
        private readonly IAssistRuleIndexSerializer _serializer;
        private readonly ITextFileStore _fileStore;
        private readonly PathOptions _pathOptions;
        private readonly ILogger<ClientAssistRuleIndexManager> _logger;

        public ClientAssistRuleIndexManager(
            IRelationshipRepository relationshipRepository,
            IResourceTypeValuesRepository resourceTypeValuesRepository,
            ITRCAssistRuleIndexService trcAssistRuleIndexService,
            IAssistRuleIndexSerializer serializer,
            ITextFileStore fileStore,
            IOptions<PathOptions> pathOptions,
            ILogger<ClientAssistRuleIndexManager> logger)
        {
            _relationshipRepository = relationshipRepository ?? throw new ArgumentNullException(nameof(relationshipRepository));
            _resourceTypeValuesRepository = resourceTypeValuesRepository ?? throw new ArgumentNullException(nameof(resourceTypeValuesRepository));
            _trcAssistRuleIndexService = trcAssistRuleIndexService ?? throw new ArgumentNullException(nameof(trcAssistRuleIndexService));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _fileStore = fileStore ?? throw new ArgumentNullException(nameof(fileStore));
            _pathOptions = pathOptions?.Value ?? throw new ArgumentNullException(nameof(pathOptions));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // ---------------- IClientAssistRuleIndexManager ----------------

        public async Task<IReadOnlyList<AssistRuleIndexEntry>> BuildAndWriteAsync(IEnumerable<Guid> libraryGuids)
        {
            var configuredPath = GetConfiguredPathOrThrow();

            try
            {
                if (libraryGuids == null) throw new ArgumentNullException(nameof(libraryGuids));
                var libs = libraryGuids.Where(g => g != Guid.Empty).Distinct().ToList();
                if (libs.Count == 0)
                    throw new ArgumentException("At least one non-empty library guid is required.", nameof(libraryGuids));

                _logger.LogInformation(
                    "BuildAndWrite Client AssistRules index started. Path={Path}, Libraries={LibCount}",
                    configuredPath, libs.Count);

                var entries = await BuildEntriesAsync(() => _resourceTypeValuesRepository.GetByLibraryIdsAsync(libs))
                    .ConfigureAwait(false);

                await WriteYamlAsync(configuredPath, entries).ConfigureAwait(false);

                _logger.LogInformation(
                    "Client AssistRules index YAML written successfully. Path={Path}, Entries={Count}",
                    configuredPath, entries.Count);

                return entries;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error while building/writing Client AssistRules index YAML. Path={Path}",
                    configuredPath);

                throw new Exception("Something went wrong while writing client assist-rules index.", ex);
            }
        }

        public async Task<IReadOnlyList<AssistRuleIndexEntry>> BuildAndWriteAsync()
        {
            var configuredPath = GetConfiguredPathOrThrow();

            try
            {
                _logger.LogInformation(
                    "BuildAndWrite Client AssistRules index (global) started. Path={Path}",
                    configuredPath);

                var entries = await BuildEntriesAsync(() => _resourceTypeValuesRepository.GetAllAsync())
                    .ConfigureAwait(false);

                await WriteYamlAsync(configuredPath, entries).ConfigureAwait(false);

                _logger.LogInformation(
                    "Client AssistRules index YAML written successfully. Path={Path}, Entries={Count}",
                    configuredPath, entries.Count);

                return entries;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error while building/writing Client AssistRules index YAML (global). Path={Path}",
                    configuredPath);

                throw new Exception("Something went wrong while writing client assist-rules index.", ex);
            }
        }

        public async Task<IReadOnlyList<AssistRuleIndexEntry>> ReloadFromYamlAsync()
        {
            var configuredPath = GetConfiguredPathOrThrow();

            try
            {
                _logger.LogInformation(
                    "Reload Client AssistRules index started. Path={Path}",
                    configuredPath);

                var yaml = await _fileStore.ReadAllTextAsync(configuredPath).ConfigureAwait(false);
                var entries = _serializer.Deserialize(yaml);

                _logger.LogInformation(
                    "Client AssistRules index reloaded successfully. Path={Path}, Entries={Count}",
                    configuredPath, entries.Count);

                return entries.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error while reloading Client AssistRules index from YAML. Path={Path}",
                    configuredPath);

                throw new Exception("Something went wrong while reloading client assist-rules index.", ex);
            }
        }

        // ---------------- Helpers (DRY core) ----------------

        private async Task<IReadOnlyList<AssistRuleIndexEntry>> BuildEntriesAsync(
            Func<Task<IEnumerable<ResourceTypeValues>>> rtvFetcher)
        {
            try
            {
                if (rtvFetcher == null) throw new ArgumentNullException(nameof(rtvFetcher));

                int maxAssignedId = await _trcAssistRuleIndexService.GetMaxAssignedIdAsync().ConfigureAwait(false);
                int idCounter = maxAssignedId + 1;

                var relationships = await _relationshipRepository.GetAllRelationshipsAsync().ConfigureAwait(false);
                var rtvs = await rtvFetcher().ConfigureAwait(false) ?? Enumerable.Empty<ResourceTypeValues>();

                var relationshipEntries = new List<AssistRuleIndexEntry>();

                foreach (var r in relationships)
                {
                    var id = await _trcAssistRuleIndexService.GetIdByRelationshipGuidAsync(r.Guid).ConfigureAwait(false);
                    if (idCounter < 0)
                    {
                        id = idCounter++;
                    }
                    relationshipEntries.Add(new AssistRuleIndexEntry
                    {
                        Type = AssistRuleType.Relationship,
                        Identity = r.Guid.ToString(),
                        LibraryGuid = Guid.Empty,
                        Id = id
                    });
                }

                var rtvEntries = new List<AssistRuleIndexEntry>();

                foreach (var v in rtvs)
                {
                    var id = await _trcAssistRuleIndexService.GetIdByResourceTypeValueAsync(v.ResourceTypeValue).ConfigureAwait(false);
                    if (id < 0)
                    {
                        id = idCounter++;
                    }

                    rtvEntries.Add(new AssistRuleIndexEntry
                    {
                        Type = AssistRuleType.ResourceTypeValues,
                        Identity = v.ResourceTypeValue,
                        LibraryGuid = v.LibraryId,
                        Id = id
                    });
                }


                var all = relationshipEntries
                    .Concat(rtvEntries)
                    .OrderBy(e => e.Type)
                    .ThenBy(e => e.LibraryGuid)
                    .ThenBy(e => e.Identity, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                _logger.LogInformation("Client AssistRules index built. Entries={Count}", all.Count);
                return all;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while building Client AssistRules index.");
                throw new Exception("Something went wrong while building client assist-rules index.", ex);
            }
        }

        private async Task WriteYamlAsync(string configuredPath, IReadOnlyList<AssistRuleIndexEntry> entries)
        {
            if (entries == null) throw new ArgumentNullException(nameof(entries));

            var yaml = _serializer.Serialize(entries);
            await _fileStore.WriteAllTextAsync(configuredPath, yaml).ConfigureAwait(false);
        }

        private string GetConfiguredPathOrThrow()
        {
            var configuredPath = _pathOptions.AssistRuleIndexYaml;
            if (string.IsNullOrWhiteSpace(configuredPath))
                throw new ArgumentException(
                    "AssistRuleIndexYaml is not configured in PathOptions.",
                    nameof(_pathOptions.AssistRuleIndexYaml));
            return configuredPath;
        }
    }
}
