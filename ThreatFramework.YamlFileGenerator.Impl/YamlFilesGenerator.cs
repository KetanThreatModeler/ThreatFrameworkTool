using ThreatFramework.Core.Models.IndexModel;
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
        private readonly IIndexService _indexService;
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

        public YamlFilesGenerator(
            IThreatRepository threatRepository,
            IComponentRepository componentRepository,
            ILibraryRepository libraryRepository,
            ISecurityRequirementRepository securityRequirementRepository,
            IPropertyRepository propertyRepository,
            IPropertyOptionRepository propertyOptionRepository,
            ITestcaseRepository testcaseRepository,
            IIndexService indexService,
            IComponentThreatMappingRepository componentThreatMappingRepository,
            IComponentSecurityRequirementMappingRepository componentSecurityRequirementMappingRepository,
            IComponentThreatSecurityRequirementMappingRepository componentThreatSecurityRequirementMappingRepository,
            IThreatSecurityRequirementMappingRepository threatSecurityRequirementMappingRepository,
            IComponentPropertyMappingRepository componentPropertyMappingRepository,
            IComponentPropertyOptionMappingRepository componentPropertyOptionMappingRepository,
            IComponentPropertyOptionThreatMappingRepository componentPropertyOptionThreatMappingRepository,
            IComponentPropertyOptionThreatSecurityRequirementMappingRepository componentPropertyOptionThreatSecurityRequirementMappingRepository
        )
        {
            _threatRepository = threatRepository ?? throw new ArgumentNullException(nameof(threatRepository));
            _componentRepository = componentRepository ?? throw new ArgumentNullException(nameof(componentRepository));
            _libraryRepository = libraryRepository ?? throw new ArgumentNullException(nameof(libraryRepository));
            _securityRequirementRepository = securityRequirementRepository ?? throw new ArgumentNullException(nameof(securityRequirementRepository));
            _propertyRepository = propertyRepository ?? throw new ArgumentNullException(nameof(propertyRepository));
            _propertyOptionRepository = propertyOptionRepository ?? throw new ArgumentNullException(nameof(propertyOptionRepository));
            _testcaseRepository = testcaseRepository ?? throw new ArgumentNullException(nameof(testcaseRepository));
            _indexService = indexService ?? throw new ArgumentNullException(nameof(indexService));
            _componentThreatMappingRepository = componentThreatMappingRepository ?? throw new ArgumentNullException(nameof(componentThreatMappingRepository));
            _componentLibraryMappingRepository = componentSecurityRequirementMappingRepository ?? throw new ArgumentNullException(nameof(componentSecurityRequirementMappingRepository));
            _componentThreatSecurityRequirementMappingRepository = componentThreatSecurityRequirementMappingRepository ?? throw new ArgumentNullException(nameof(componentThreatSecurityRequirementMappingRepository));
            _threatSecurityRequirementMappingRepository = threatSecurityRequirementMappingRepository ?? throw new ArgumentNullException(nameof(threatSecurityRequirementMappingRepository));
            _componentPropertyMappingRepository = componentPropertyMappingRepository ?? throw new ArgumentNullException(nameof(componentPropertyMappingRepository));
            _componentPropertyOptionMappingRepository = componentPropertyOptionMappingRepository ?? throw new ArgumentNullException(nameof(componentPropertyOptionMappingRepository));
            _componentPropertyOptionThreatMappingRepository = componentPropertyOptionThreatMappingRepository ?? throw new ArgumentNullException(nameof(componentPropertyOptionThreatMappingRepository));
            _componentPropertyOptionThreatSecurityRequirementMappingRepository = componentPropertyOptionThreatSecurityRequirementMappingRepository ?? throw new ArgumentNullException(nameof(componentPropertyOptionThreatSecurityRequirementMappingRepository));
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

            foreach (var item in items)
            {
                var fileName = fileNameGenerator(item);
                var filePath = Path.Combine(path, fileName);
                var yamlContent = yamlGenerator(item);

                await File.WriteAllTextAsync(filePath, yamlContent);
                fileCount++;
            }

            return (path, fileCount);
        }

        public async Task<(string path, int fileCount)> GenerateYamlFilesForSpecificLibraries(string path, List<Guid> libraryIds)
        {
            var libraries = await _libraryRepository.GetLibrariesByGuidsAsync(libraryIds);
            
            return await GenerateYamlFiles(
                path,
                libraries,
                lib => $"{_indexService.GetIdByKindAndGuid(EntityKind.Library, lib.Guid)}.yaml",
                libraryItem => LibraryTemplate.Generate(libraryItem)  
            );
        }

        public async Task<(string path, int fileCount)> GenerateYamlFilesForSpecificThreats(string path, List<Guid> libraryIds)
        {
            var threats = await _threatRepository.GetThreatsByLibraryIdAsync(libraryIds);
            return await GenerateYamlFiles(
                path,
                threats,
                threat => $"{_indexService.GetIdByKindAndGuid(EntityKind.Threat, threat.Guid)}.yaml",
                threatItem => ThreatTemplate.Generate(threatItem));
        }

        public async Task<(string path, int fileCount)> GenerateYamlFilesForSpecificComponents(string path, List<Guid> libraryIds)
        {
            var components = await _componentRepository.GetComponentsByLibraryIdAsync(libraryIds);
            return await GenerateYamlFiles(
                path,
                components,
                component => $"{_indexService.GetIdByKindAndGuid(EntityKind.Component, component.Guid)}.yaml",
                componentItem => ComponentTemplate.Generate(componentItem));
        }

        public async Task<(string path, int fileCount)> GenerateYamlFilesForSpecificSecurityRequirements(string path, List<Guid> libraryIds)
        {
            var securityRequirements = await _securityRequirementRepository.GetSecurityRequirementsByLibraryIdAsync(libraryIds);
            return await GenerateYamlFiles(
                path,
                securityRequirements,
                sr => $"{_indexService.GetIdByKindAndGuid(EntityKind.SecurityRequirement, sr.Guid)}.yaml",
                srItem => SecurityRequirementTemplate.Generate(srItem));
        }

        public async Task<(string path, int fileCount)> GenerateYamlFilesForSpecificTestCases(string path, List<Guid> libraryIds)
        {
            var testCases = await _testcaseRepository.GetTestcasesByLibraryIdAsync(libraryIds);
            return await GenerateYamlFiles(
                path,
                testCases,
                tc => $"{_indexService.GetIdByKindAndGuid(EntityKind.TestCase, tc.Guid)}.yaml",
                tcItem => TestCaseTemplate.Generate(tcItem));
        }

        public async Task<(string path, int fileCount)> GenerateYamlFilesForSpecificProperties(string path, List<Guid> propertyIds)
        {
            var properties = await _propertyRepository.GetPropertiesByLibraryIdAsync(propertyIds);
            return await GenerateYamlFiles(
                path,
                properties,
                prop => $"{_indexService.GetIdByKindAndGuid(EntityKind.Property, prop.Guid)}.yaml",
                propItem => PropertyTemplate.Generate(propItem));
        }

        public async Task<(string path, int fileCount)> GenerateYamlFilesForSpecificPropertyOptions(string path)
        {
            var properties = await _propertyOptionRepository.GetAllPropertyOptionsAsync();
            return await GenerateYamlFiles(
                path,
                properties,
                prop => $"{_indexService.GetIdByKindAndGuid(EntityKind.PropertyOption, prop.Guid)}.yaml",
                propItem => PropertyOptionTemplate.Generate(propItem));
        }

        public async Task<(string path, int fileCount)> GenerateYamlFilesForComponentSecurityRequirementMappings(string path, List<Guid> libraryIds)
        {
            var mappings = await _componentLibraryMappingRepository.GetMappingsByLibraryIdAsync(libraryIds);
            return await GenerateYamlFiles(
                path,
                mappings,
                mapping => $"{_indexService.GetIdByKindAndGuid(EntityKind.Component, mapping.ComponentGuid) + FileNameSeperator + _indexService.GetIdByKindAndGuid(EntityKind.SecurityRequirement, mapping.SecurityRequirementGuid)}.yaml",
                mappingItem => CSRMappingTemplate.Generate(mappingItem));
        }

        public async Task<(string path, int fileCount)> GenerateYamlFilesForComponentThreatMappings(string path, List<Guid> libraryIds)
        {
            var mappings = await _componentThreatMappingRepository.GetMappingsByLibraryIdAsync(libraryIds);
            return await GenerateYamlFiles(
                path,
                mappings,
                mapping => $"{_indexService.GetIdByKindAndGuid(EntityKind.Component, mapping.ComponentGuid) + FileNameSeperator + _indexService.GetIdByKindAndGuid(EntityKind.Threat, mapping.ThreatGuid)}.yaml",
                mappingItem => CTHMappingTemplate.Generate(mappingItem));
        }

        public async Task<(string path, int fileCount)> GenerateYamlFilesForComponentThreatSecurityRequirementMappings(string path, List<Guid> libraryIds)
        {
            var mappings = await _componentThreatSecurityRequirementMappingRepository.GetMappingsByLibraryIdAsync(libraryIds);
            return await GenerateYamlFiles(
                path,
                mappings,
                mapping => $"{_indexService.GetIdByKindAndGuid(EntityKind.Component, mapping.ComponentGuid) + FileNameSeperator + _indexService.GetIdByKindAndGuid(EntityKind.Threat, mapping.ThreatGuid) + FileNameSeperator + _indexService.GetIdByKindAndGuid(EntityKind.SecurityRequirement, mapping.SecurityRequirementGuid)}.yaml",
                mappingItem => CTSRMappingTemplate.Generate(mappingItem));
        }

        public async Task<(string path, int fileCount)> GenerateYamlFilesForThreatSecurityRequirementMappings(string path, List<Guid> libraryIds)
        {
           var mappings = await _threatSecurityRequirementMappingRepository.GetMappingsByLibraryIdAsync(libraryIds);
            return await GenerateYamlFiles(
                path,
                mappings,
                mapping => $"{_indexService.GetIdByKindAndGuid(EntityKind.Threat, mapping.ThreatGuid) + FileNameSeperator + _indexService.GetIdByKindAndGuid(EntityKind.SecurityRequirement, mapping.SecurityRequirementGuid)}.yaml",
                mappingItem => TSRMappingTemplate.Generate(mappingItem));
        }

        public async Task<(string path, int fileCount)> GenerateYamlFilesForComponentPropertyMappings(string path, List<Guid> libraryIds)
        {
           var mappings = await _componentPropertyMappingRepository.GetMappingsByLibraryIdAsync(libraryIds);
            return await GenerateYamlFiles(
                path,
                mappings,
                mapping => $"{_indexService.GetIdByKindAndGuid(EntityKind.Component, mapping.ComponentGuid) + FileNameSeperator + _indexService.GetIdByKindAndGuid(EntityKind.Property, mapping.PropertyGuid)}.yaml",
                mappingItem => CPTemplate.Generate(mappingItem));
        }

        public async Task<(string path, int fileCount)> GenerateYamlFilesForComponentPropertyOptionMappings(string path, List<Guid> libraryIds)
        {
            var mappings = await _componentPropertyOptionMappingRepository.GetMappingsByLibraryGuidAsync(libraryIds);
            return await GenerateYamlFiles(
                path,
                mappings,
                mapping => $"{_indexService.GetIdByKindAndGuid(EntityKind.Component, mapping.ComponentGuid) + FileNameSeperator + _indexService.GetIdByKindAndGuid(EntityKind.Property, mapping.PropertyGuid) + FileNameSeperator + _indexService.GetIdByKindAndGuid(EntityKind.PropertyOption, mapping.PropertyOptionGuid)}.yaml",
                mappingItem => CPOTemplate.Generate(mappingItem));
        }

        public async Task<(string path, int fileCount)> GenerateYamlFilesForComponentPropertyOptionThreatMappings(string path, List<Guid> libraryIds)
        {
            var mappings = await _componentPropertyOptionThreatMappingRepository.GetMappingsByLibraryGuidAsync(libraryIds);
            return await GenerateYamlFiles(
                path,
                mappings,
                mapping => $"{_indexService.GetIdByKindAndGuid(EntityKind.Component, mapping.ComponentGuid) + FileNameSeperator + _indexService.GetIdByKindAndGuid(EntityKind.Property, mapping.PropertyGuid) + FileNameSeperator + _indexService.GetIdByKindAndGuid(EntityKind.PropertyOption, mapping.PropertyOptionGuid) + FileNameSeperator + _indexService.GetIdByKindAndGuid(EntityKind.Threat, mapping.ThreatGuid)}.yaml",
                mappingItem => CPOThreatTemplate.Generate(mappingItem));
        }

        public async Task<(string path, int fileCount)> GenerateYamlFilesForComponentPropertyOptionThreatSecurityRequirementMappings(string path, List<Guid> libraryIds)
        {
            var mappings = await _componentPropertyOptionThreatSecurityRequirementMappingRepository.GetMappingsByLibraryGuidAsync(libraryIds);  
            return await GenerateYamlFiles(
                path,
                mappings,
                mapping => $"{_indexService.GetIdByKindAndGuid(EntityKind.Component, mapping.ComponentGuid) + FileNameSeperator + _indexService.GetIdByKindAndGuid(EntityKind.Property, mapping.PropertyGuid) + FileNameSeperator + _indexService.GetIdByKindAndGuid(EntityKind.PropertyOption, mapping.PropertyOptionGuid) + FileNameSeperator + _indexService.GetIdByKindAndGuid(EntityKind.Threat, mapping.ThreatGuid) + FileNameSeperator + _indexService.GetIdByKindAndGuid(EntityKind.SecurityRequirement, mapping.SecurityRequirementGuid)}.yaml",
                mappingItem => CPOTSRTemplate.Generate(mappingItem));
        }
    }
    }
