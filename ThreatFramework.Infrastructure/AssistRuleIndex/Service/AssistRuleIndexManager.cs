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
using ThreatModeler.TF.Infra.Contract.AssistRuleIndex.Service;
using ThreatModeler.TF.Infra.Contract.Repository.AssistRules;

namespace ThreatModeler.TF.Infra.Implmentation.AssistRuleIndex.Service
{
    public sealed class AssistRuleIndexManager : IAssistRuleIndexManager
    {
        private const string RelationshipPrefix = "REL";
        private const string ResourceTypeValuePrefix = "RTV";

        private readonly IRelationshipRepository _relationshipRepository;
        private readonly IResourceTypeValuesRepository _resourceTypeValuesRepository;

        private readonly AssistRuleIndexCache _cache;
        private readonly IAssistRuleIndexIdGenerator _idGenerator;
        private readonly IAssistRuleIndexSerializer _serializer;
        private readonly ITextFileStore _fileStore;
        private readonly PathOptions _pathOptions;
        private readonly ILogger<AssistRuleIndexManager> _logger;

        public AssistRuleIndexManager(
            IRelationshipRepository relationshipRepository,
            IResourceTypeValuesRepository resourceTypeValuesRepository,
            AssistRuleIndexCache cache,
            IAssistRuleIndexIdGenerator idGenerator,
            IAssistRuleIndexSerializer serializer,
            ITextFileStore fileStore,
            IOptions<PathOptions> pathOptions,
            ILogger<AssistRuleIndexManager> logger)
        {
            _relationshipRepository = relationshipRepository ?? throw new ArgumentNullException(nameof(relationshipRepository));
            _resourceTypeValuesRepository = resourceTypeValuesRepository ?? throw new ArgumentNullException(nameof(resourceTypeValuesRepository));

            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _idGenerator = idGenerator ?? throw new ArgumentNullException(nameof(idGenerator));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _fileStore = fileStore ?? throw new ArgumentNullException(nameof(fileStore));
            _pathOptions = pathOptions?.Value ?? throw new ArgumentNullException(nameof(pathOptions));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IReadOnlyList<AssistRuleIndexEntry>> BuildAsync(IEnumerable<Guid> libraryGuids)
        {
            try
            {

                _idGenerator.Reset();

                var relationshipsTask = _relationshipRepository.GetAllRelationshipsAsync();
                var rtvTask = GetResourceTypeValuesAsync(libraryGuids);

                await Task.WhenAll(relationshipsTask, rtvTask);

                var relationshipEntries = relationshipsTask.Result.Select(r => new AssistRuleIndexEntry
                {
                    Type = AssistRuleType.Relationship,
                    Identity = r.Guid.ToString(),
                    LibraryGuid = Guid.Empty,
                    Id = _idGenerator.Next(RelationshipPrefix)
                });

                var rtvEntries = rtvTask.Result.Select(v => new AssistRuleIndexEntry
                {
                    Type = AssistRuleType.ResourceTypeValues,
                    Identity = v.ResourceTypeValue,
                    LibraryGuid = v.LibraryId,
                    Id = _idGenerator.Next(ResourceTypeValuePrefix)
                });

                var all = relationshipEntries
                    .Concat(rtvEntries)
                    .OrderBy(e => e.Type)
                    .ThenBy(e => e.LibraryGuid)
                    .ThenBy(e => e.Identity, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                _cache.ReplaceAll(all);

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

        public async Task ReloadFromYamlAsync()
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

                _cache.ReplaceAll(entries);

                _logger.LogInformation(
                    "AssistRules index reloaded successfully. Path: {Path}, Entries: {Count}",
                    configuredPath, entries.Count);
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
                return await _resourceTypeValuesRepository.GetAllAsync();

            return await _resourceTypeValuesRepository.GetByLibraryIdsAsync(libraryGuids.ToList());
        }
    }
}
