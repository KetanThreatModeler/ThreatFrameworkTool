using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ThreatFramework.Infra.Contract.Index;
using ThreatFramework.Infra.Contract.Repository;
using ThreatFramework.YamlFileGenerator.Contract;
using ThreatFramework.YamlFileGenerator.Impl.Templates;
using ThreatFramework.YamlFileGenerator.Impl.Templates.ComponentMapping;
using ThreatFramework.YamlFileGenerator.Impl.Templates.PropertyMapping;
using ThreatModeler.TF.Git.Contract.Common;
using ThreatModeler.TF.Infra.Contract.Repository.CoreEntities;
using ThreatModeler.TF.Infra.Contract.Repository.Global;
using ThreatModeler.TF.Infra.Contract.Repository.ThreatMapping;
using ThreatModeler.TF.YamlFileGenerator.Implementation.Templates;
using ThreatModeler.TF.YamlFileGenerator.Implementation.Templates.Global;

namespace ThreatFramework.YamlFileGenerator.Impl
{
    public class YamlFilesGenerator : IYamlFileGenerator
    {
        private const string FileNameSeperator = "_";
        private readonly ILogger<YamlFilesGenerator> _logger;
        private readonly ILibraryRepository _libraryRepository;
        private readonly IThreatRepository _threatRepository;
        private readonly IComponentRepository _componentRepository;
        private readonly IComponentTypeRepository _componentTypeRepository;
        private readonly ISecurityRequirementRepository _securityRequirementRepository;
        private readonly ITestcaseRepository _testcaseRepository;
        private readonly IPropertyRepository _propertyRepository;
        private readonly IPropertyTypeRepository _propertyTypeRepository;
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
            IComponentTypeRepository componentTypeRepository,
            ILibraryRepository libraryRepository,
            ISecurityRequirementRepository securityRequirementRepository,
            IPropertyRepository propertyRepository,
            IPropertyTypeRepository propertyTypeRepository,
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
            _componentTypeRepository = componentTypeRepository ?? throw new ArgumentNullException(nameof(componentTypeRepository));
            _libraryRepository = libraryRepository ?? throw new ArgumentNullException(nameof(libraryRepository));
            _securityRequirementRepository = securityRequirementRepository ?? throw new ArgumentNullException(nameof(securityRequirementRepository));
            _propertyRepository = propertyRepository ?? throw new ArgumentNullException(nameof(propertyRepository));
            _propertyTypeRepository = propertyTypeRepository ?? throw new ArgumentNullException(nameof(propertyTypeRepository));
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

            ValidatePath(path);
            EnsureDirectoryExists(path);

            int totalFileCount = 0;
            _logger.LogInformation("Starting YAML file generation in path: {Path}", path);

            foreach (var library in libraries)
            {
                var libraryIdFolder = _indexService.GetInt(library.Guid).ToString();
                var libraryFolderPath = Path.Combine(path, libraryIdFolder);
                EnsureDirectoryExists(libraryFolderPath);

                var fileName = $"{_indexService.GetInt(library.Guid)}.yaml";
                var filePath = Path.Combine(libraryFolderPath, fileName);
                var yamlContent = LibraryTemplate.Generate(library);

                await File.WriteAllTextAsync(filePath, yamlContent);
                totalFileCount++;
            }

            _logger.LogInformation("Generated {FileCount} YAML files in path: {Path}", totalFileCount, path);
            return (path, totalFileCount);
        }

        public async Task<(string path, int fileCount)> GenerateYamlFilesForSpecificThreats(string path, List<Guid> libraryIds)
        {
            _logger.LogInformation("Generating YAML files for threats from {LibraryCount} libraries", libraryIds.Count);

            ValidatePath(path);
            EnsureDirectoryExists(path);

            int totalFileCount = 0;
            _logger.LogInformation("Starting YAML file generation in path: {Path}", path);

            // Get all threats for the specified libraries
            var threats = await _threatRepository.GetThreatsByLibraryIdAsync(libraryIds);

            // Group threats by library
            var threatsGroupedByLibrary = threats.GroupBy(t => t.LibraryGuid);

            foreach (var libraryGroup in threatsGroupedByLibrary)
            {
                var libraryGuid = libraryGroup.Key;
                var libraryIdFolder = _indexService.GetInt(libraryGuid).ToString();
                var libraryFolderPath = Path.Combine(path, libraryIdFolder, FolderNames.Threats);
                EnsureDirectoryExists(libraryFolderPath);

                foreach (var threat in libraryGroup)
                {
                    var fileName = $"{_indexService.GetInt(threat.Guid)}.yaml";
                    var filePath = Path.Combine(libraryFolderPath, fileName);
                    var yamlContent = ThreatTemplate.Generate(threat);

                    await File.WriteAllTextAsync(filePath, yamlContent);
                    totalFileCount++;
                }
            }

            _logger.LogInformation("Generated {FileCount} YAML files in path: {Path}", totalFileCount, path);
            return (path, totalFileCount);
        }

        public async Task<(string path, int fileCount)> GenerateYamlFilesForSpecificComponents(string path, List<Guid> libraryIds)
        {
            _logger.LogInformation("Generating YAML files for components from {LibraryCount} libraries", libraryIds.Count);

            ValidatePath(path);
            EnsureDirectoryExists(path);

            int totalFileCount = 0;
            _logger.LogInformation("Starting YAML file generation in path: {Path}", path);

            // Get all components for the specified libraries
            var components = await _componentRepository.GetComponentsByLibraryIdAsync(libraryIds);

            // Group components by library
            var componentsGroupedByLibrary = components.GroupBy(c => c.LibraryGuid);

            foreach (var libraryGroup in componentsGroupedByLibrary)
            {
                var libraryGuid = libraryGroup.Key;
                var libraryIdFolder = _indexService.GetInt(libraryGuid).ToString();
                var libraryFolderPath = Path.Combine(path, libraryIdFolder, FolderNames.Components);
                EnsureDirectoryExists(libraryFolderPath);

                foreach (var component in libraryGroup)
                {
                    var fileName = $"{_indexService.GetInt(component.Guid)}.yaml";
                    var filePath = Path.Combine(libraryFolderPath, fileName);
                    var yamlContent = ComponentTemplate.Generate(component);

                    await File.WriteAllTextAsync(filePath, yamlContent);
                    totalFileCount++;
                }
            }

            _logger.LogInformation("Generated {FileCount} YAML files in path: {Path}", totalFileCount, path);
            return (path, totalFileCount);
        }

        public async Task<(string path, int fileCount)> GenerateYamlFilesForSpecificSecurityRequirements(string path, List<Guid> libraryIds)
        {
            _logger.LogInformation("Generating YAML files for security requirements from {LibraryCount} libraries", libraryIds.Count);

            ValidatePath(path);
            EnsureDirectoryExists(path);

            int totalFileCount = 0;
            _logger.LogInformation("Starting YAML file generation in path: {Path}", path);

            // Get all security requirements for the specified libraries
            var securityRequirements = await _securityRequirementRepository.GetSecurityRequirementsByLibraryIdAsync(libraryIds);

            // Group security requirements by library
            var securityRequirementsGroupedByLibrary = securityRequirements.GroupBy(sr => sr.LibraryId);

            foreach (var libraryGroup in securityRequirementsGroupedByLibrary)
            {
                var libraryGuid = libraryGroup.Key;
                var libraryIdFolder = _indexService.GetInt(libraryGuid).ToString();
                var libraryFolderPath = Path.Combine(path, libraryIdFolder, FolderNames.SecurityRequirements);
                EnsureDirectoryExists(libraryFolderPath);

                foreach (var securityRequirement in libraryGroup)
                {
                    var fileName = $"{_indexService.GetInt(securityRequirement.Guid)}.yaml";
                    var filePath = Path.Combine(libraryFolderPath, fileName);
                    var yamlContent = SecurityRequirementTemplate.Generate(securityRequirement);

                    await File.WriteAllTextAsync(filePath, yamlContent);
                    totalFileCount++;
                }
            }

            _logger.LogInformation("Generated {FileCount} YAML files in path: {Path}", totalFileCount, path);
            return (path, totalFileCount);
        }

        public async Task<(string path, int fileCount)> GenerateYamlFilesForSpecificTestCases(string path, List<Guid> libraryIds)
        {
            _logger.LogInformation("Generating YAML files for test cases from {LibraryCount} libraries", libraryIds.Count);

            ValidatePath(path);
            EnsureDirectoryExists(path);

            int totalFileCount = 0;
            _logger.LogInformation("Starting YAML file generation in path: {Path}", path);

            // Get all test cases for the specified libraries
            var testCases = await _testcaseRepository.GetTestcasesByLibraryIdAsync(libraryIds);

            // Group test cases by library
            var testCasesGroupedByLibrary = testCases.GroupBy(tc => tc.LibraryId);

            foreach (var libraryGroup in testCasesGroupedByLibrary)
            {
                var libraryGuid = libraryGroup.Key;
                var libraryIdFolder = _indexService.GetInt(libraryGuid).ToString();
                var libraryFolderPath = Path.Combine(path, libraryIdFolder, FolderNames.TestCases);
                EnsureDirectoryExists(libraryFolderPath);

                foreach (var testCase in libraryGroup)
                {
                    var fileName = $"{_indexService.GetInt(testCase.Guid)}.yaml";
                    var filePath = Path.Combine(libraryFolderPath, fileName);
                    var yamlContent = TestCaseTemplate.Generate(testCase);

                    await File.WriteAllTextAsync(filePath, yamlContent);
                    totalFileCount++;
                }
            }

            _logger.LogInformation("Generated {FileCount} YAML files in path: {Path}", totalFileCount, path);
            return (path, totalFileCount);
        }

        public async Task<(string path, int fileCount)> GenerateYamlFilesForSpecificProperties(string path, List<Guid> libraryIds)
        {
            _logger.LogInformation("Generating YAML files for properties from {LibraryCount} libraries", libraryIds.Count);

            ValidatePath(path);
            EnsureDirectoryExists(path);

            int totalFileCount = 0;
            _logger.LogInformation("Starting YAML file generation in path: {Path}", path);

            // Get all properties for the specified libraries
            var properties = await _propertyRepository.GetPropertiesByLibraryIdAsync(libraryIds);

            // Group properties by library
            var propertiesGroupedByLibrary = properties.GroupBy(p => p.LibraryGuid);

            foreach (var libraryGroup in propertiesGroupedByLibrary)
            {
                var libraryGuid = libraryGroup.Key;
                var libraryIdFolder = _indexService.GetInt(libraryGuid).ToString();
                var libraryFolderPath = Path.Combine(path, libraryIdFolder, FolderNames.Properties);
                EnsureDirectoryExists(libraryFolderPath);

                foreach (var property in libraryGroup)
                {
                    var fileName = $"{_indexService.GetInt(property.Guid)}.yaml";
                    var filePath = Path.Combine(libraryFolderPath, fileName);
                    var yamlContent = PropertyTemplate.Generate(property);

                    await File.WriteAllTextAsync(filePath, yamlContent);
                    totalFileCount++;
                }
            }

            _logger.LogInformation("Generated {FileCount} YAML files in path: {Path}", totalFileCount, path);
            return (path, totalFileCount);
        }

        public async Task<(string path, int fileCount)> GenerateYamlFilesForPropertyOptions(string path)
        {
            _logger.LogInformation("Generating YAML files for all property options");

            ValidatePath(path);

            var globalFolderPath = Path.Combine(path, "global", FolderNames.PropertyOptions);
            EnsureDirectoryExists(globalFolderPath);

            var propertyOptions = await _propertyOptionRepository.GetAllPropertyOptionsAsync();
            return await GenerateYamlFiles(
                globalFolderPath,
                propertyOptions,
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

        public async Task<(string path, int fileCount)> GenerateYamlFilesForAllComponentTypes(string path)
        {
            var globalFolderPath = Path.Combine(path, "global", FolderNames.ComponentType);
            EnsureDirectoryExists(globalFolderPath);
            _logger.LogInformation("Generating YAML files for componentTypes");
            var componentTypes = await _componentTypeRepository.GetComponentTypesAsync();
            return await GenerateYamlFiles(
                globalFolderPath,
                componentTypes,
                componentType => $"{_indexService.GetInt(componentType.Guid)}.yaml",
                componentType => ComponentTypeTemplate.Generate(componentType));
        }

        public async Task<(string path, int fileCount)> GenerateYamlFilesForPropertyTypes(string path)
        {
            var globalFolderPath = Path.Combine(path, "global", FolderNames.PropertyType);
            EnsureDirectoryExists(globalFolderPath);
            _logger.LogInformation("Generating YAML files for PropertyTypes");
            var propertyTypes = await _propertyTypeRepository.GetAllPropertyTypeAsync();
            return await GenerateYamlFiles(
                globalFolderPath,
                propertyTypes,
                propertyType => $"{_indexService.GetInt(propertyType.Guid)}.yaml",
                propertyType => PropertyTypeTemplate.Generate(propertyType));
        }
    }
}
