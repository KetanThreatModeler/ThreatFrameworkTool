using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ThreatFramework.Infra.Contract.Index;
using ThreatFramework.Infra.Contract.Repository;
using ThreatFramework.YamlFileGenerator.Contract;
using ThreatModeler.TF.Core.Config;
using ThreatModeler.TF.Infra.Contract.Repository;

namespace ThreatFramework.YamlFileGenerator.Impl
{
    public sealed class TrcYamlFilesGenerator : IYamlFilesGeneratorForTRC
    {
        private readonly ILogger<TrcYamlFilesGenerator> _logger;
        private readonly ILogger<YamlFilesGenerator> _yamlLogger;
        private readonly IRepositoryHub _hub;
        private readonly IGuidIndexService _indexService;
        private readonly ILibraryRepository _libraryRepository;
        private readonly PathOptions _options;

        public TrcYamlFilesGenerator(
            ILogger<TrcYamlFilesGenerator> logger,
            ILogger<YamlFilesGenerator> yamlLogger,
            IRepositoryHubFactory hubFactory,
            IOptions<PathOptions> options,
            ILibraryRepository libraryRepository,
            IGuidIndexService indexService)
        {
            _logger = logger;
            _yamlLogger = yamlLogger;
            _hub = hubFactory.Create(DataPlane.Trc);
            _indexService = indexService;
            _libraryRepository = libraryRepository;
            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task GenerateAsync(string outputFolderPath, List<Guid> libraryIds)
        {
            if (string.IsNullOrWhiteSpace(outputFolderPath))
                throw new ArgumentException("Output path is required.", nameof(outputFolderPath));

            var cfg = _options.TrcOutput ?? throw new InvalidOperationException("YamlExport:Trc is not configured.");
          

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

            var root = outputFolderPath;

            await gen.GenerateYamlFilesForSpecificLibraries(root, libraryIds);
            await gen.GenerateYamlFilesForSpecificComponents(root, libraryIds);
            await gen.GenerateYamlFilesForAllComponentTypes(root);
            await gen.GenerateYamlFilesForSpecificThreats(root, libraryIds);
            await gen.GenerateYamlFilesForSpecificSecurityRequirements(root, libraryIds);
            await gen.GenerateYamlFilesForSpecificProperties(root, libraryIds);
            await gen.GenerateYamlFilesForPropertyTypes(root);
            await gen.GenerateYamlFilesForSpecificTestCases(root, libraryIds);
            await gen.GenerateYamlFilesForPropertyOptions(root);

            await gen.GenerateYamlFilesForComponentSecurityRequirementMappings(Path.Combine(root, "mappings", "component-security-requirement"), libraryIds);
            await gen.GenerateYamlFilesForComponentThreatMappings(Path.Combine(root, "mappings", "component-threat"), libraryIds);
            await gen.GenerateYamlFilesForComponentThreatSecurityRequirementMappings(Path.Combine(root, "mappings", "component-threat-security-requirement"), libraryIds);
            await gen.GenerateYamlFilesForThreatSecurityRequirementMappings(Path.Combine(root, "mappings", "threat-security-requirement"), libraryIds);
            await gen.GenerateYamlFilesForComponentPropertyMappings(Path.Combine(root, "mappings", "component-property"), libraryIds);
            await gen.GenerateYamlFilesForComponentPropertyOptionMappings(Path.Combine(root, "mappings", "component-property-option"), libraryIds);
            await gen.GenerateYamlFilesForComponentPropertyOptionThreatMappings(Path.Combine(root, "mappings", "component-property-option-threat"), libraryIds);
            await gen.GenerateYamlFilesForComponentPropertyOptionThreatSecurityRequirementMappings(Path.Combine(root, "mappings", "component-property-option-threat-security-requirement"), libraryIds);

            _logger.LogInformation("TRC export completed to {Root}.", root);
        }

        public async Task GenerateForLibraryIdsAsync(string outputFolderPath, IEnumerable<Guid> libraryIds)
        {
            await GenerateAsync(outputFolderPath, libraryIds.ToList());
        }

        public async Task GenerateForReadOnlyLibraryAsync(string outputFolderPath)
        {
            var libraryCaches = await _hub.Libraries.GetLibrariesCacheAsync();
           await GenerateAsync(outputFolderPath, libraryCaches.Where(_ => _.IsReadonly).Select(_ => _.Guid).ToList());
        }
    }
}
