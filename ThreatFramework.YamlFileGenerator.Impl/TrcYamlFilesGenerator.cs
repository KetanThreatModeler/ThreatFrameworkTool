using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ThreatFramework.Core.Config;
using ThreatFramework.Infra.Contract.Index;
using ThreatFramework.Infra.Contract.Repository;
using ThreatFramework.YamlFileGenerator.Contract;

namespace ThreatFramework.YamlFileGenerator.Impl
{
    public sealed class TrcYamlFilesGenerator : IYamlFilesGeneratorForTRC
    {
        private readonly ILogger<TrcYamlFilesGenerator> _logger;
        private readonly ILogger<YamlFilesGenerator> _yamlLogger;
        private readonly IRepositoryHubFactory _hubFactory;
        private readonly IGuidIndexService _indexService;
        private readonly YamlExportOptions _options;

        public TrcYamlFilesGenerator(
            ILogger<TrcYamlFilesGenerator> logger,
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

            var cfg = _options.Trc ?? throw new InvalidOperationException("YamlExport:Trc is not configured.");
            if (cfg.LibraryIds is null || cfg.LibraryIds.Count == 0)
                throw new InvalidOperationException("YamlExport:Trc:LibraryIds must contain at least one GUID.");

            Directory.CreateDirectory(outputFolderPath);

            // plane-scoped repository hub for TRC
            var hub = _hubFactory.Create(DataPlane.Trc);

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

            // Entities
            await gen.GenerateYamlFilesForSpecificLibraries(Path.Combine(root, "libraries"), ids);
            await gen.GenerateYamlFilesForSpecificComponents(Path.Combine(root, "components"), ids);
            await gen.GenerateYamlFilesForSpecificThreats(Path.Combine(root, "threats"), ids);
            await gen.GenerateYamlFilesForSpecificSecurityRequirements(Path.Combine(root, "security-requirements"), ids);
            await gen.GenerateYamlFilesForSpecificProperties(Path.Combine(root, "properties"), ids);
            await gen.GenerateYamlFilesForSpecificPropertyOptions(Path.Combine(root, "property-options"));
            await gen.GenerateYamlFilesForSpecificTestCases(Path.Combine(root, "test-cases"), ids);

            // Mappings (all for TRC)
            await gen.GenerateYamlFilesForComponentSecurityRequirementMappings(Path.Combine(root, "mappings", "component-security-requirement"), ids);
            await gen.GenerateYamlFilesForComponentThreatMappings(Path.Combine(root, "mappings", "component-threat"), ids);
            await gen.GenerateYamlFilesForComponentThreatSecurityRequirementMappings(Path.Combine(root, "mappings", "component-threat-security-requirement"), ids);
            await gen.GenerateYamlFilesForThreatSecurityRequirementMappings(Path.Combine(root, "mappings", "threat-security-requirement"), ids);
            await gen.GenerateYamlFilesForComponentPropertyMappings(Path.Combine(root, "mappings", "component-property"), ids);
            await gen.GenerateYamlFilesForComponentPropertyOptionMappings(Path.Combine(root, "mappings", "component-property-option"), ids);
            await gen.GenerateYamlFilesForComponentPropertyOptionThreatMappings(Path.Combine(root, "mappings", "component-property-option-threat"), ids);
            await gen.GenerateYamlFilesForComponentPropertyOptionThreatSecurityRequirementMappings(Path.Combine(root, "mappings", "component-property-option-threat-security-requirement"), ids);

            _logger.LogInformation("TRC export completed to {Root}.", root);
        }
    }
}
