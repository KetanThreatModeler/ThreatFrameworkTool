using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ThreatFramework.Core.Config;
using ThreatFramework.Infra.Contract.Index;
using ThreatFramework.Infra.Contract.Repository;
using ThreatFramework.YamlFileGenerator.Contract;

namespace ThreatFramework.YamlFileGenerator.Impl
{
    public sealed class ClientYamlFilesGenerator : IYamlFileGeneratorForClient
    {
        private readonly ILogger<ClientYamlFilesGenerator> _logger;
        private readonly ILogger<YamlFilesGenerator> _yamlLogger;
        private readonly IRepositoryHubFactory _hubFactory;
        private readonly IGuidIndexService _indexService;
        private readonly YamlExportOptions _options;

        public ClientYamlFilesGenerator(
            ILogger<ClientYamlFilesGenerator> logger,
            ILogger<YamlFilesGenerator> yamlLogger,
            IRepositoryHubFactory hubFactory,
            IOptions<YamlExportOptions> options,
            IGuidIndexService indexService)
        {
            _logger = logger;
            _yamlLogger = yamlLogger;
            _hubFactory = hubFactory;
            _indexService = indexService;
            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task GenerateAsync(string outputFolderPath)
        {
            if (string.IsNullOrWhiteSpace(outputFolderPath))
                throw new ArgumentException("Output path is required.", nameof(outputFolderPath));

            var cfg = _options.Client ?? throw new InvalidOperationException("YamlExport:Client is not configured.");
            if (cfg.LibraryIds is null || cfg.LibraryIds.Count == 0)
                throw new InvalidOperationException("YamlExport:Client:LibraryIds must contain at least one GUID.");

            Directory.CreateDirectory(outputFolderPath);

            // plane-scoped repository hub for Client
            var hub = _hubFactory.Create(DataPlane.Client);

            // construct your existing generator with plane-specific repos
            var gen = new YamlFilesGenerator(
                logger: _yamlLogger,
                threatRepository: hub.Threats,
                componentRepository: hub.Components,
                libraryRepository: hub.Libraries,
                securityRequirementRepository: hub.SecurityRequirements,
                propertyRepository: hub.Properties,
                propertyOptionRepository: hub.PropertyOptions,
                testcaseRepository: hub.Testcases,
                componentThreatMappingRepository: hub.ComponentThreatMappings,
                componentSecurityRequirementMappingRepository: hub.ComponentSecurityRequirementMappings,
                componentThreatSecurityRequirementMappingRepository: hub.ComponentThreatSecurityRequirementMappings,
                threatSecurityRequirementMappingRepository: hub.ThreatSecurityRequirementMappings,
                componentPropertyMappingRepository: hub.ComponentPropertyMappings,
                componentPropertyOptionMappingRepository: hub.ComponentPropertyOptionMappings,
                componentPropertyOptionThreatMappingRepository: hub.ComponentPropertyOptionThreatMappings,
                componentPropertyOptionThreatSecurityRequirementMappingRepository: hub.ComponentPropertyOptionThreatSecurityRequirementMappings,
                indexService: _indexService
            );

            var ids = cfg.LibraryIds;
            var root = outputFolderPath;

            // Hard-coded entity selection for CLIENT
            await gen.GenerateYamlFilesForSpecificLibraries(Path.Combine(root, "libraries"), ids);
            await gen.GenerateYamlFilesForSpecificComponents(Path.Combine(root, "components"), ids);
            await gen.GenerateYamlFilesForSpecificThreats(Path.Combine(root, "threats"), ids);
            await gen.GenerateYamlFilesForSpecificSecurityRequirements(Path.Combine(root, "security-requirements"), ids);
            await gen.GenerateYamlFilesForSpecificProperties(Path.Combine(root, "properties"), ids);
            await gen.GenerateYamlFilesForSpecificPropertyOptions(Path.Combine(root, "property-options"));
            await gen.GenerateYamlFilesForSpecificTestCases(Path.Combine(root, "test-cases"), ids);

            _logger.LogInformation("Client export completed to {Root}.", root);
        }
    }
}
