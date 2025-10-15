using Microsoft.Extensions.Logging;
using ThreatFramework.Core.IndexModel;
using ThreatFramework.Infra.Contract.Index;
using ThreatFramework.Infra.Contract.Repository;
using ThreatFramework.YamlFileGenerator.Contract;
using ThreatFramework.YamlFileGenerator.Impl.Templates;
using ThreatFramework.YamlFileGenerator.Impl.Templates.ComponentMapping;
using ThreatFramework.YamlFileGenerator.Impl.Templates.PropertyMapping;

namespace ThreatFramework.YamlFileGenerator.Impl
{
    public class YamlFilesGenerator : IYamlFileGenerator
    {
        private const string YamlFileExtension = ".yaml";
        private const string FileNameSeperator = "_";
        private readonly ILogger<YamlFilesGenerator> _logger;
        private readonly ILibraryRepository _libraryRepository;
        private readonly IThreatRepository _threatRepository;
        private readonly IComponentRepository _componentRepository;
        private readonly ISecurityRequirementRepository _securityRequirementRepository;
        private readonly ITestcaseRepository _testcaseRepository;
        private readonly IPropertyRepository _propertyRepository;
        private readonly IPropertyOptionRepository _propertyOptionRepository;
        private readonly IComponentSecurityRequirementMappingRepository _componentLibraryMappingRepository;
        private readonly IComponentThreatMappingRepository _componentThreatMappingRepository;
        private readonly IComponentThreatSecurityRequirementMappingRepository _componentThreatSecurityRequirementMappingRepository;
        private readonly IThreatSecurityRequirementMappingRepository _threatSecurityRequirementMappingRepository;
        private readonly IComponentPropertyMappingRepository _componentPropertyMappingRepository;
        private readonly IComponentPropertyOptionMappingRepository _componentPropertyOptionMappingRepository;
        private readonly IComponentPropertyOptionThreatMappingRepository _componentPropertyOptionThreatMappingRepository;
        private readonly IComponentPropertyOptionThreatSecurityRequirementMappingRepository _componentPropertyOptionThreatSecurityRequirementMappingRepository;
        private readonly IGuidIndexService _indexService;

        public YamlFilesGenerator(
            ILogger<YamlFilesGenerator> logger,
            IThreatRepository threatRepository,
            IComponentRepository componentRepository,
            ILibraryRepository libraryRepository,
            ISecurityRequirementRepository securityRequirementRepository,
            IPropertyRepository propertyRepository,
            IPropertyOptionRepository propertyOptionRepository,
            ITestcaseRepository testcaseRepository,
            IComponentThreatMappingRepository componentThreatMappingRepository,
            IComponentSecurityRequirementMappingRepository componentSecurityRequirementMappingRepository,
            IComponentThreatSecurityRequirementMappingRepository componentThreatSecurityRequirementMappingRepository,
            IThreatSecurityRequirementMappingRepository threatSecurityRequirementMappingRepository,
            IComponentPropertyMappingRepository componentPropertyMappingRepository,
            IComponentPropertyOptionMappingRepository componentPropertyOptionMappingRepository,
            IComponentPropertyOptionThreatMappingRepository componentPropertyOptionThreatMappingRepository,
            IComponentPropertyOptionThreatSecurityRequirementMappingRepository componentPropertyOptionThreatSecurityRequirementMappingRepository,
            IGuidIndexService indexService
        )
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _threatRepository = threatRepository ?? throw new ArgumentNullException(nameof(threatRepository));
            _componentRepository = componentRepository ?? throw new ArgumentNullException(nameof(componentRepository));
            _libraryRepository = libraryRepository ?? throw new ArgumentNullException(nameof(libraryRepository));
            _securityRequirementRepository = securityRequirementRepository ?? throw new ArgumentNullException(nameof(securityRequirementRepository));
            _propertyRepository = propertyRepository ?? throw new ArgumentNullException(nameof(propertyRepository));
            _propertyOptionRepository = propertyOptionRepository ?? throw new ArgumentNullException(nameof(propertyOptionRepository));
            _testcaseRepository = testcaseRepository ?? throw new ArgumentNullException(nameof(testcaseRepository));
            _componentThreatMappingRepository = componentThreatMappingRepository ?? throw new ArgumentNullException(nameof(componentThreatMappingRepository));
            _componentLibraryMappingRepository = componentSecurityRequirementMappingRepository ?? throw new ArgumentNullException(nameof(componentSecurityRequirementMappingRepository));
            _componentThreatSecurityRequirementMappingRepository = componentThreatSecurityRequirementMappingRepository ?? throw new ArgumentNullException(nameof(componentThreatSecurityRequirementMappingRepository));
            _threatSecurityRequirementMappingRepository = threatSecurityRequirementMappingRepository ?? throw new ArgumentNullException(nameof(threatSecurityRequirementMappingRepository));
            _componentPropertyMappingRepository = componentPropertyMappingRepository ?? throw new ArgumentNullException(nameof(componentPropertyMappingRepository));
            _componentPropertyOptionMappingRepository = componentPropertyOptionMappingRepository ?? throw new ArgumentNullException(nameof(componentPropertyOptionMappingRepository));
            _componentPropertyOptionThreatMappingRepository = componentPropertyOptionThreatMappingRepository ?? throw new ArgumentNullException(nameof(componentPropertyOptionThreatMappingRepository));
            _componentPropertyOptionThreatSecurityRequirementMappingRepository = componentPropertyOptionThreatSecurityRequirementMappingRepository ?? throw new ArgumentNullException(nameof(componentPropertyOptionThreatSecurityRequirementMappingRepository));
            _indexService = indexService ?? throw new ArgumentNullException(nameof(indexService));
            _logger.LogInformation("YamlFilesGenerator initialized successfully");
        }

        private static void ValidatePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path cannot be null or empty", nameof(path));
        }

        private static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        private async Task<(string path, int fileCount)> GenerateYamlFiles<T>(
            string path,
            IEnumerable<T> items,
            Func<T, string> fileNameGenerator,
            Func<T, string> yamlGenerator)
        {
            ValidatePath(path);
            EnsureDirectoryExists(path);

            int fileCount = 0;
            _logger.LogInformation("Starting YAML file generation in path: {Path}", path);

            foreach (var item in items)
            {
                var fileName = fileNameGenerator(item);
                var filePath = Path.Combine(path, fileName);
                var yamlContent = yamlGenerator(item);

                await File.WriteAllTextAsync(filePath, yamlContent);
                fileCount++;
            }

            _logger.LogInformation("Generated {FileCount} YAML files in path: {Path}", fileCount, path);
            return (path, fileCount);
        }

        public async Task<(string path, int fileCount)> GenerateYamlFilesForSpecificLibraries(string path, List<Guid> libraryIds)
        {
            _logger.LogInformation("Generating YAML files for {LibraryCount} libraries", libraryIds.Count);
            var libraries = await _libraryRepository.GetLibrariesByGuidsAsync(libraryIds);
            
            return await GenerateYamlFiles(
                path,
                libraries,
                lib => $"{_indexService.GetInt(lib.Guid)}.yaml",
                libraryItem => LibraryTemplate.Generate(libraryItem)  
            );
        }

        public async Task<(string path, int fileCount)> GenerateYamlFilesForSpecificThreats(string path, List<Guid> libraryIds)
        {
            _logger.LogInformation("Generating YAML files for threats from {LibraryCount} libraries", libraryIds.Count);
            var threats = await _threatRepository.GetThreatsByLibraryIdAsync(libraryIds);
            return await GenerateYamlFiles(
                path,
                threats,
                threat => $"{_indexService.GetInt(threat.Guid)}.yaml",
                threatItem => ThreatTemplate.Generate(threatItem));
        }

        public async Task<(string path, int fileCount)> GenerateYamlFilesForSpecificComponents(string path, List<Guid> libraryIds)
        {
            _logger.LogInformation("Generating YAML files for components from {LibraryCount} libraries", libraryIds.Count);
            var components = await _componentRepository.GetComponentsByLibraryIdAsync(libraryIds);
            return await GenerateYamlFiles(
                path,
                components,
                component => $"{_indexService.GetInt(component.Guid)}.yaml",
                componentItem => ComponentTemplate.Generate(componentItem));
        }

        public async Task<(string path, int fileCount)> GenerateYamlFilesForSpecificSecurityRequirements(string path, List<Guid> libraryIds)
        {
            _logger.LogInformation("Generating YAML files for security requirements from {LibraryCount} libraries", libraryIds.Count);
            var securityRequirements = await _securityRequirementRepository.GetSecurityRequirementsByLibraryIdAsync(libraryIds);
            return await GenerateYamlFiles(
                path,
                securityRequirements,
                sr => $"{_indexService.GetInt(sr.Guid)}.yaml",
                srItem => SecurityRequirementTemplate.Generate(srItem));
        }

        public async Task<(string path, int fileCount)> GenerateYamlFilesForSpecificTestCases(string path, List<Guid> libraryIds)
        {
            _logger.LogInformation("Generating YAML files for test cases from {LibraryCount} libraries", libraryIds.Count);
            var testCases = await _testcaseRepository.GetTestcasesByLibraryIdAsync(libraryIds);
            return await GenerateYamlFiles(
                path,
                testCases,
                tc => $"{_indexService.GetInt(tc.Guid)}.yaml",
                tcItem => TestCaseTemplate.Generate(tcItem));
        }

        public async Task<(string path, int fileCount)> GenerateYamlFilesForSpecificProperties(string path, List<Guid> propertyIds)
        {
            _logger.LogInformation("Generating YAML files for properties from {LibraryCount} libraries", propertyIds.Count);
            var properties = await _propertyRepository.GetPropertiesByLibraryIdAsync(propertyIds);
            return await GenerateYamlFiles(
                path,
                properties,
                prop => $"{_indexService.GetInt(prop.Guid)}.yaml",
                propItem => PropertyTemplate.Generate(propItem));
        }

        public async Task<(string path, int fileCount)> GenerateYamlFilesForSpecificPropertyOptions(string path)
        {
            _logger.LogInformation("Generating YAML files for all property options");
            var properties = await _propertyOptionRepository.GetAllPropertyOptionsAsync();
            return await GenerateYamlFiles(
                path,
                properties,
                prop => $"{_indexService.GetInt(prop.Guid)}.yaml",
                propItem => PropertyOptionTemplate.Generate(propItem));
        }

        public async Task<(string path, int fileCount)> GenerateYamlFilesForComponentSecurityRequirementMappings(string path, List<Guid> libraryIds)
        {
            _logger.LogInformation("Generating YAML files for component-security requirement mappings from {LibraryCount} libraries", libraryIds.Count);
            var mappings = await _componentLibraryMappingRepository.GetMappingsByLibraryIdAsync(libraryIds);
            return await GenerateYamlFiles(
                path,
                mappings,
                mapping => $"{_indexService.GetInt(mapping.ComponentGuid) + FileNameSeperator + _indexService.GetInt(mapping.SecurityRequirementGuid)}.yaml",
                mappingItem => CSRMappingTemplate.Generate(mappingItem));
        }

        public async Task<(string path, int fileCount)> GenerateYamlFilesForComponentThreatMappings(string path, List<Guid> libraryIds)
        {
            _logger.LogInformation("Generating YAML files for component-threat mappings from {LibraryCount} libraries", libraryIds.Count);
            var mappings = await _componentThreatMappingRepository.GetMappingsByLibraryIdAsync(libraryIds);
            return await GenerateYamlFiles(
                path,
                mappings,
                mapping => $"{_indexService.GetInt(mapping.ComponentGuid) + FileNameSeperator + _indexService.GetInt(mapping.ThreatGuid)}.yaml",
                mappingItem => CTHMappingTemplate.Generate(mappingItem));
        }

        public async Task<(string path, int fileCount)> GenerateYamlFilesForComponentThreatSecurityRequirementMappings(string path, List<Guid> libraryIds)
        {
            _logger.LogInformation("Generating YAML files for component-threat-security requirement mappings from {LibraryCount} libraries", libraryIds.Count);
            var mappings = await _componentThreatSecurityRequirementMappingRepository.GetMappingsByLibraryIdAsync(libraryIds);
            return await GenerateYamlFiles(
                path,
                mappings,
                mapping => $"{_indexService.GetInt(mapping.ComponentGuid) + FileNameSeperator + _indexService.GetInt(mapping.ThreatGuid) + FileNameSeperator + _indexService.GetInt(mapping.SecurityRequirementGuid)}.yaml",
                mappingItem => CTSRMappingTemplate.Generate(mappingItem));
        }

        public async Task<(string path, int fileCount)> GenerateYamlFilesForThreatSecurityRequirementMappings(string path, List<Guid> libraryIds)
        {
            _logger.LogInformation("Generating YAML files for threat-security requirement mappings from {LibraryCount} libraries", libraryIds.Count);
            var mappings = await _threatSecurityRequirementMappingRepository.GetMappingsByLibraryIdAsync(libraryIds);
            return await GenerateYamlFiles(
                path,
                mappings,
                mapping => $"{_indexService.GetInt(mapping.ThreatGuid) + FileNameSeperator + _indexService.GetInt(mapping.SecurityRequirementGuid)}.yaml",
                mappingItem => TSRMappingTemplate.Generate(mappingItem));
        }

        public async Task<(string path, int fileCount)> GenerateYamlFilesForComponentPropertyMappings(string path, List<Guid> libraryIds)
        {
            _logger.LogInformation("Generating YAML files for component-property mappings from {LibraryCount} libraries", libraryIds.Count);
            var mappings = await _componentPropertyMappingRepository.GetMappingsByLibraryIdAsync(libraryIds);
            return await GenerateYamlFiles(
                path,
                mappings,
                mapping => $"{_indexService.GetInt(mapping.ComponentGuid) + FileNameSeperator + _indexService.GetInt(mapping.PropertyGuid)}.yaml",
                mappingItem => CPTemplate.Generate(mappingItem));
        }

        public async Task<(string path, int fileCount)> GenerateYamlFilesForComponentPropertyOptionMappings(string path, List<Guid> libraryIds)
        {
            _logger.LogInformation("Generating YAML files for component-property-option mappings from {LibraryCount} libraries", libraryIds.Count);
            var mappings = await _componentPropertyOptionMappingRepository.GetMappingsByLibraryGuidAsync(libraryIds);
            return await GenerateYamlFiles(
                path,
                mappings,
                mapping => $"{_indexService.GetInt(mapping.ComponentGuid) + FileNameSeperator + _indexService.GetInt(mapping.PropertyGuid) + FileNameSeperator + _indexService.GetInt(mapping.PropertyOptionGuid)}.yaml",
                mappingItem => CPOTemplate.Generate(mappingItem));
        }

        public async Task<(string path, int fileCount)> GenerateYamlFilesForComponentPropertyOptionThreatMappings(string path, List<Guid> libraryIds)
        {
            _logger.LogInformation("Generating YAML files for component-property-option-threat mappings from {LibraryCount} libraries", libraryIds.Count);
            var mappings = await _componentPropertyOptionThreatMappingRepository.GetMappingsByLibraryGuidAsync(libraryIds);
            return await GenerateYamlFiles(
                path,
                mappings,
                mapping => $"{_indexService.GetInt(mapping.ComponentGuid) + FileNameSeperator + _indexService.GetInt(mapping.PropertyGuid) + FileNameSeperator + _indexService.GetInt(mapping.PropertyOptionGuid) + FileNameSeperator + _indexService.GetInt(mapping.ThreatGuid)}.yaml",
                mappingItem => CPOThreatTemplate.Generate(mappingItem));
        }

        public async Task<(string path, int fileCount)> GenerateYamlFilesForComponentPropertyOptionThreatSecurityRequirementMappings(string path, List<Guid> libraryIds)
        {
            _logger.LogInformation("Generating YAML files for component-property-option-threat-security requirement mappings from {LibraryCount} libraries", libraryIds.Count);
            var mappings = await _componentPropertyOptionThreatSecurityRequirementMappingRepository.GetMappingsByLibraryGuidAsync(libraryIds);  
            return await GenerateYamlFiles(
                path,
                mappings,
                mapping => $"{_indexService.GetInt(mapping.ComponentGuid) + FileNameSeperator + _indexService.GetInt(mapping.PropertyGuid) + FileNameSeperator + _indexService.GetInt(mapping.PropertyOptionGuid) + FileNameSeperator + _indexService.GetInt(mapping.ThreatGuid) + FileNameSeperator + _indexService.GetInt(mapping.SecurityRequirementGuid)}.yaml",
                mappingItem => CPOTSRTemplate.Generate(mappingItem));
        }

        public async Task GenerateFilesToPathAsync(string path)
        {
            _logger.LogInformation("Starting complete YAML file generation to path: {Path}", path);
            
            try
            {
                ValidatePath(path);
                EnsureDirectoryExists(path);

                // Get all libraries to extract library IDs
                _logger.LogInformation("Retrieving all readonly libraries...");
                var allLibraries = await _libraryRepository.GetReadonlyLibrariesAsync();
                var libraryIds = allLibraries.Select(lib => lib.Guid).ToList();
                _logger.LogInformation("Found {LibraryCount} libraries for YAML generation", libraryIds.Count);

                if (!libraryIds.Any())
                {
                    _logger.LogWarning("No libraries found for YAML generation. Skipping all generation steps.");
                    return;
                }

                // Generate files for each entity type in separate folders
                _logger.LogInformation("Starting entity file generation...");
                
                try
                {
                    _logger.LogInformation("Generating library files...");
                    await GenerateYamlFilesForSpecificLibraries(Path.Combine(path, "libraries"), libraryIds);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to generate library files");
                    throw;
                }

                try
                {
                    _logger.LogInformation("Generating threat files...");
                    await GenerateYamlFilesForSpecificThreats(Path.Combine(path, "threats"), libraryIds);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to generate threat files");
                    throw;
                }

                try
                {
                    _logger.LogInformation("Generating component files...");
                    await GenerateYamlFilesForSpecificComponents(Path.Combine(path, "components"), libraryIds);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to generate component files");
                    throw;
                }

                try
                {
                    _logger.LogInformation("Generating security requirement files...");
                    await GenerateYamlFilesForSpecificSecurityRequirements(Path.Combine(path, "security-requirements"), libraryIds);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to generate security requirement files");
                    throw;
                }

                try
                {
                    _logger.LogInformation("Generating test case files...");
                    await GenerateYamlFilesForSpecificTestCases(Path.Combine(path, "test-cases"), libraryIds);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to generate test case files");
                    throw;
                }

                try
                {
                    _logger.LogInformation("Generating property files...");
                    await GenerateYamlFilesForSpecificProperties(Path.Combine(path, "properties"), libraryIds);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to generate property files");
                    throw;
                }

                try
                {
                    _logger.LogInformation("Generating property option files...");
                    await GenerateYamlFilesForSpecificPropertyOptions(Path.Combine(path, "property-options"));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to generate property option files");
                    throw;
                }
                
                // Generate mapping files
                _logger.LogInformation("Starting mapping file generation...");
                
                try
                {
                    _logger.LogInformation("Generating component-security requirement mapping files...");
                    await GenerateYamlFilesForComponentSecurityRequirementMappings(Path.Combine(path, "mappings", "component-security-requirement"), libraryIds);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to generate component-security requirement mapping files");
                    throw;
                }

                try
                {
                    _logger.LogInformation("Generating component-threat mapping files...");
                    await GenerateYamlFilesForComponentThreatMappings(Path.Combine(path, "mappings", "component-threat"), libraryIds);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to generate component-threat mapping files");
                    throw;
                }

                try
                {
                    _logger.LogInformation("Generating component-threat-security requirement mapping files...");
                    await GenerateYamlFilesForComponentThreatSecurityRequirementMappings(Path.Combine(path, "mappings", "component-threat-security-requirement"), libraryIds);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to generate component-threat-security requirement mapping files");
                    throw;
                }

                try
                {
                    _logger.LogInformation("Generating threat-security requirement mapping files...");
                    await GenerateYamlFilesForThreatSecurityRequirementMappings(Path.Combine(path, "mappings", "threat-security-requirement"), libraryIds);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to generate threat-security requirement mapping files");
                    throw;
                }

                try
                {
                    _logger.LogInformation("Generating component-property mapping files...");
                    await GenerateYamlFilesForComponentPropertyMappings(Path.Combine(path, "mappings", "component-property"), libraryIds);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to generate component-property mapping files - this is likely where the 'ComponentProperty' table error occurs");
                    throw;
                }

                try
                {
                    _logger.LogInformation("Generating component-property-option mapping files...");
                    await GenerateYamlFilesForComponentPropertyOptionMappings(Path.Combine(path, "mappings", "component-property-option"), libraryIds);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to generate component-property-option mapping files");
                    throw;
                }

                try
                {
                    _logger.LogInformation("Generating component-property-option-threat mapping files...");
                    await GenerateYamlFilesForComponentPropertyOptionThreatMappings(Path.Combine(path, "mappings", "component-property-option-threat"), libraryIds);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to generate component-property-option-threat mapping files");
                    throw;
                }

                try
                {
                    _logger.LogInformation("Generating component-property-option-threat-security requirement mapping files...");
                    await GenerateYamlFilesForComponentPropertyOptionThreatSecurityRequirementMappings(Path.Combine(path, "mappings", "component-property-option-threat-security-requirement"), libraryIds);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to generate component-property-option-threat-security requirement mapping files");
                    throw;
                }
                
                _logger.LogInformation("Completed YAML file generation to path: {Path}", path);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error during YAML file generation to path: {Path}", path);
                throw;
            }
        }
    }
}
