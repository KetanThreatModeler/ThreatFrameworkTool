using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ThreatFramework.Infra.Contract.Index;
using ThreatFramework.Infra.Contract.Repository;
using ThreatFramework.YamlFileGenerator.Contract;
using ThreatModeler.TF.Core.Model.CoreEntities;
using ThreatModeler.TF.Infra.Contract.Repository;

namespace ThreatFramework.YamlFileGenerator.Impl
{
    public sealed class ClientYamlFilesGenerator : IYamlFileGeneratorForClient
    {
        private readonly ILogger<ClientYamlFilesGenerator> _logger;
        private readonly ILogger<YamlFilesGenerator> _yamlLogger;
        private readonly IRepositoryHub _hub;
        private readonly IGuidIndexService _indexService;
        private readonly PathOptions _options;

        public ClientYamlFilesGenerator(
            ILogger<ClientYamlFilesGenerator> logger,
            ILogger<YamlFilesGenerator> yamlLogger,
            IRepositoryHubFactory hubFactory,
            IOptions<PathOptions> options,
            IGuidIndexService indexService)
        {
            _logger = logger;
            _yamlLogger = yamlLogger;
            _hub = hubFactory.Create(DataPlane.Client);
            _indexService = indexService;
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
                indexService: _indexService
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

            _logger.LogInformation("Client export completed to {Root}.", outputFolderPath);
        }
    }
}
