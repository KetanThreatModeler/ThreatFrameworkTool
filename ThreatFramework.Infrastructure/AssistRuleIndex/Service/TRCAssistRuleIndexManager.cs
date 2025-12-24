using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThreatModeler.TF.Core.Model.AssistRules;
using ThreatModeler.TF.Core.Model.CoreEntities;
using ThreatModeler.TF.Infra.Contract.AssistRuleIndex.Builder;
using ThreatModeler.TF.Infra.Contract.AssistRuleIndex.Model;
using ThreatModeler.TF.Infra.Contract.AssistRuleIndex.TRC;
using ThreatModeler.TF.Infra.Contract.Repository.AssistRules;

namespace ThreatModeler.TF.Infra.Implmentation.AssistRuleIndex.Service
{
    public sealed class TRCAssistRuleIndexManager : ITRCAssistRuleIndexManager
    {

        private readonly IRelationshipRepository _relationshipRepository;
        private readonly IResourceTypeValuesRepository _resourceTypeValuesRepository;

        private readonly IAssistRuleIndexSerializer _serializer;
        private readonly ITextFileStore _fileStore;
        private readonly PathOptions _pathOptions;
        private readonly ILogger<TRCAssistRuleIndexManager> _logger;

        public TRCAssistRuleIndexManager(
            IRelationshipRepository relationshipRepository,
            IResourceTypeValuesRepository resourceTypeValuesRepository,
            IAssistRuleIndexSerializer serializer,
            ITextFileStore fileStore,
            IOptions<PathOptions> pathOptions,
            ILogger<TRCAssistRuleIndexManager> logger)
        {
            _relationshipRepository = relationshipRepository ?? throw new ArgumentNullException(nameof(relationshipRepository));
            _resourceTypeValuesRepository = resourceTypeValuesRepository ?? throw new ArgumentNullException(nameof(resourceTypeValuesRepository));

            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _fileStore = fileStore ?? throw new ArgumentNullException(nameof(fileStore));
            _pathOptions = pathOptions?.Value ?? throw new ArgumentNullException(nameof(pathOptions));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IReadOnlyList<AssistRuleIndexEntry>> BuildAsync(IEnumerable<Guid> libraryGuids)
        {
            try
            {
                int idCounter = 1;
                var relationships = await _relationshipRepository.GetAllRelationshipsAsync();
                var rtvs = await GetResourceTypeValuesAsync(libraryGuids);

                var relationshipEntries = relationships.Select(r => new AssistRuleIndexEntry
                {
                    Type = AssistRuleType.Relationship,
                    Identity = r.Guid.ToString(),
                    LibraryGuid = Guid.Empty,
                    Id = idCounter++
                });

                var rtvEntries = rtvs.Select(v => new AssistRuleIndexEntry
                {
                    Type = AssistRuleType.ResourceTypeValues,
                    Identity = v.ResourceTypeValue,
                    LibraryGuid = v.LibraryId,
                    Id = idCounter++
                });

                var all = relationshipEntries
                    .Concat(rtvEntries)
                    .OrderBy(e => e.Type)
                    .ThenBy(e => e.LibraryGuid)
                    .ThenBy(e => e.Identity, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                _logger.LogInformation("AssistRules index built. Entries: {Count}", all.Count);
                return all;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while building AssistRules index.");
                throw new Exception("Something went wrong while building assist-rules index.", ex);
            }
        }

        public async Task<IReadOnlyList<AssistRuleIndexEntry>> BuildAndWriteAsync(
            IEnumerable<Guid> libraryGuids)
        {
            // Always use config path, ignore incoming yamlFilePath
            var configuredPath = _pathOptions.AssistRuleIndexYaml;

            try
            {
                if (string.IsNullOrWhiteSpace(configuredPath))
                    throw new ArgumentException("AssistRuleIndexYaml is not configured in PathOptions.", nameof(_pathOptions.AssistRuleIndexYaml));

                _logger.LogInformation(
                    "BuildAndWrite AssistRules index started. Using configured path: {Path}. Incoming path ignored.",
                    configuredPath);

                var entries = await BuildAsync(libraryGuids);

                var yaml = _serializer.Serialize(entries);
                await _fileStore.WriteAllTextAsync(configuredPath, yaml);

                _logger.LogInformation(
                    "AssistRules index YAML written successfully. Path: {Path}, Entries: {Count}",
                    configuredPath, entries.Count);

                return entries;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error while building/writing AssistRules index YAML. ConfiguredPath={Path}",
                    configuredPath);

                throw new Exception("Something went wrong while writing assist-rules index.", ex);
            }
        }

        public async Task<IReadOnlyList<AssistRuleIndexEntry>> ReloadFromYamlAsync()
        {
            // Always use config path, ignore incoming yamlFilePath
            var configuredPath = _pathOptions.AssistRuleIndexYaml;

            try
            {
                if (string.IsNullOrWhiteSpace(configuredPath))
                    throw new ArgumentException("AssistRuleIndexYaml is not configured in PathOptions.", nameof(_pathOptions.AssistRuleIndexYaml));

                _logger.LogInformation(
                    "Reload AssistRules index started. Using configured path: {Path}. Incoming path ignored.",
                    configuredPath);

                var yaml = await _fileStore.ReadAllTextAsync(configuredPath);
                var entries = _serializer.Deserialize(yaml);

                _logger.LogInformation(
                    "AssistRules index reloaded successfully. Path: {Path}, Entries: {Count}",
                    configuredPath, entries.Count);
                return entries.ToList();

            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error while reloading AssistRules index from YAML. ConfiguredPath={Path}",
                    configuredPath);

                throw new Exception("Something went wrong while reloading assist-rules index.", ex);
            }
        }

        private async Task<IEnumerable<ResourceTypeValues>> GetResourceTypeValuesAsync(IEnumerable<Guid> libraryGuids)
        {
            if (libraryGuids == null || !libraryGuids.Any())
            {
                _logger.LogError("GetResourceTypeValuesAsync called with null libraryGuids.");
                return Enumerable.Empty<ResourceTypeValues>();
            }
            return await _resourceTypeValuesRepository.GetByLibraryIdsAsync(libraryGuids.ToList());
        }

        public async Task<IReadOnlyList<AssistRuleIndexEntry>> BuildAndWriteAsync()
        {
            var configuredPath = _pathOptions.AssistRuleIndexYaml;
            int idCounter = 1;
            var relationships = await _relationshipRepository.GetAllRelationshipsAsync();
            var rtvs = await _resourceTypeValuesRepository.GetAllAsync();

            var relationshipEntries = relationships.Select(r => new AssistRuleIndexEntry
            {
                Type = AssistRuleType.Relationship,
                Identity = r.Guid.ToString(),
                LibraryGuid = Guid.Empty,
                Id = idCounter++
            });

            var rtvEntries = rtvs.Select(v => new AssistRuleIndexEntry
            {
                Type = AssistRuleType.ResourceTypeValues,
                Identity = v.ResourceTypeValue,
                LibraryGuid = v.LibraryId,
                Id = idCounter++
            });

            var all = relationshipEntries
                .Concat(rtvEntries)
                .OrderBy(e => e.Type)
                .ThenBy(e => e.LibraryGuid)
                .ThenBy(e => e.Identity, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var yaml = _serializer.Serialize(all);
            await _fileStore.WriteAllTextAsync(configuredPath, yaml);

            _logger.LogInformation(
                "AssistRules index YAML written successfully. Path: {Path}, Entries: {Count}",
                configuredPath, all.Count);

            return all;
        }
    }
}
