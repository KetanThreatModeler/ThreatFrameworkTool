using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ThreatFramework.Infra.Contract.Repository;
using ThreatFramework.YamlFileGenerator.Contract;
using ThreatModeler.TF.Core.Model.CoreEntities;
using ThreatModeler.TF.Infra.Contract.AssistRuleIndex.Service;
using ThreatModeler.TF.Infra.Contract.Index.TRC;
using ThreatModeler.TF.Infra.Contract.Repository;

namespace ThreatFramework.YamlFileGenerator.Impl
{
    public sealed class ClientYamlFilesGenerator : IYamlFileGeneratorForClient
    {
        private readonly ILogger<ClientYamlFilesGenerator> _logger;
        private readonly ILogger<YamlFilesGenerator> _yamlLogger;
        private readonly IRepositoryHub _hub;
        private readonly ITRCGuidIndexService _indexService;
        private readonly IAssistRuleIndexQuery _assistRuleIndexQuery;
        private readonly PathOptions _options;

        public ClientYamlFilesGenerator(
            ILogger<ClientYamlFilesGenerator> logger,
            ILogger<YamlFilesGenerator> yamlLogger,
            IRepositoryHubFactory hubFactory,
            IOptions<PathOptions> options,
            ITRCGuidIndexService indexService,
            IAssistRuleIndexQuery assistRuleIndexQuery)
        {
            _logger = logger;
            _yamlLogger = yamlLogger;
            _hub = hubFactory.Create(DataPlane.Client);
            _indexService = indexService;
            _assistRuleIndexQuery = assistRuleIndexQuery;
            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task GenerateForLibraryIdsAsync(string outputFolderPath, List<Guid> libraryIds)
        {
            if (string.IsNullOrWhiteSpace(outputFolderPath))
                throw new ArgumentException("Output path is required.", nameof(outputFolderPath));

            var cfg = _options.ClientOutput ?? throw new InvalidOperationException("YamlExport:Client is not configured.");

            Directory.CreateDirectory(outputFolderPath);

            // construct your existing generator with plane-specific repos
            var gen = new YamlFilesGenerator(
                logger: _yamlLogger,
                threatRepository: _hub.Threats,
                componentRepository: _hub.Components,
                componentTypeRepository: _hub.ComponentTypes,
                libraryRepository: _hub.Libraries,
                securityRequirementRepository: _hub.SecurityRequirements,
                propertyRepository: _hub.Properties,
                propertyTypeRepository: _hub.PropertyTypes,
                propertyOptionRepository: _hub.PropertyOptions,
                testcaseRepository: _hub.Testcases,
                componentThreatMappingRepository: _hub.ComponentThreatMappings,
                componentSecurityRequirementMappingRepository: _hub.ComponentSecurityRequirementMappings,
                componentThreatSecurityRequirementMappingRepository: _hub.ComponentThreatSecurityRequirementMappings,
                threatSecurityRequirementMappingRepository: _hub.ThreatSecurityRequirementMappings,
                componentPropertyMappingRepository: _hub.ComponentPropertyMappings,
                componentPropertyOptionMappingRepository: _hub.ComponentPropertyOptionMappings,
                componentPropertyOptionThreatMappingRepository: _hub.ComponentPropertyOptionThreatMappings,
                componentPropertyOptionThreatSecurityRequirementMappingRepository: _hub.ComponentPropertyOptionThreatSecurityRequirementMappings,
                indexService: _indexService,
                assistRuleIndexQuery: _assistRuleIndexQuery,
                relationshipRepository: _hub.Relationships,
                resourceTypeValuesRepository: _hub.ResourceTypeValues,
                resourceTypeValueRelationshipRepository: _hub.ResourceTypeValueRelationships
            );

            await gen.GenerateYamlFilesForSpecificLibraries(outputFolderPath, libraryIds);
            await gen.GenerateYamlFilesForSpecificComponents(outputFolderPath, libraryIds);
            await gen.GenerateYamlFilesForAllComponentTypes(outputFolderPath);
            await gen.GenerateYamlFilesForSpecificThreats(outputFolderPath, libraryIds);
            await gen.GenerateYamlFilesForSpecificSecurityRequirements(outputFolderPath, libraryIds);
            await gen.GenerateYamlFilesForSpecificProperties(outputFolderPath, libraryIds);
            await gen.GenerateYamlFilesForPropertyTypes(outputFolderPath);
            await gen.GenerateYamlFilesForSpecificTestCases(outputFolderPath, libraryIds);
            await gen.GenerateYamlFilesForPropertyOptions(outputFolderPath);
            //await gen.GenerateYamlFilesForRelationships(outputFolderPath);
            //await gen.GenerateYamlFilesForResourceTypeValues(outputFolderPath, libraryIds);
            //await gen.GenerateYamlFilesForResourceTypeValueRelationships(outputFolderPath, libraryIds);
            await GenerateMappingsAsync(gen, outputFolderPath, libraryIds);
            _logger.LogInformation("Client export completed to {Root}.", outputFolderPath);
        }

        private async Task GenerateMappingsAsync(YamlFilesGenerator gen, string root, List<Guid> libraryIds)
        {
            _logger.LogDebug("Generating Mapping YAML files...");
            var mappingsRoot = Path.Combine(root, "mappings");

            await gen.GenerateYamlFilesForComponentSecurityRequirementMappings(Path.Combine(mappingsRoot, "component-security-requirement"), libraryIds);
            await gen.GenerateYamlFilesForComponentThreatMappings(Path.Combine(mappingsRoot, "component-threat"), libraryIds);
            await gen.GenerateYamlFilesForComponentThreatSecurityRequirementMappings(Path.Combine(mappingsRoot, "component-threat-security-requirement"), libraryIds);
            await gen.GenerateYamlFilesForThreatSecurityRequirementMappings(Path.Combine(mappingsRoot, "threat-security-requirement"), libraryIds);
            await gen.GenerateYamlFilesForComponentPropertyMappings(Path.Combine(mappingsRoot, "component-property"), libraryIds);
            await gen.GenerateYamlFilesForComponentPropertyOptionMappings(Path.Combine(mappingsRoot, "component-property-option"), libraryIds);
            await gen.GenerateYamlFilesForComponentPropertyOptionThreatMappings(Path.Combine(mappingsRoot, "component-property-option-threat"), libraryIds);
            await gen.GenerateYamlFilesForComponentPropertyOptionThreatSecurityRequirementMappings(Path.Combine(mappingsRoot, "component-property-option-threat-security-requirement"), libraryIds);
        }
    }
}
