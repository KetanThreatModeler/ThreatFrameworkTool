using ThreatFramework.Infra.Contract.Index;
using ThreatFramework.Infra.Contract.Repository;
using ThreatFramework.YamlFileGenerator.Contract;
using ThreatFramework.YamlFileGenerator.Impl.Templates;

namespace ThreatFramework.YamlFileGenerator.Impl
{
    public class YamlFilesGenerator : IYamlFileGenerator
    {
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
            string kind,
            Func<T, Guid> guidSelector,
            Func<T, string> yamlGenerator)
        {
            ValidatePath(path);
            EnsureDirectoryExists(path);

            int fileCount = 0;

            foreach (var item in items)
            {
                var guid = guidSelector(item);
                var fileName = $"{_indexService.GetIdByKindAndGuid(kind, guid)}.yaml";
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
                    "library",
                    library => library.Guid,
                    library => LibraryTemplate.Generate(library)
                );
            }

            public async Task<(string path, int fileCount)> GenerateYamlFilesForSpecificThreats(string path, List<Guid> libraryIds)
            {
                var threats = await _threatRepository.GetThreatsByLibraryIdAsync(libraryIds);
                return await GenerateYamlFiles(
                    path,
                    threats,
                    "threat",
                    threat => threat.Guid,
                    threat => ThreatTemplate.Generate(threat)
                );
            }

            public async Task<(string path, int fileCount)> GenerateYamlFilesForSpecificComponents(string path, List<Guid> libraryIds)
            {
                var components = await _componentRepository.GetComponentsByLibraryIdAsync(libraryIds);
                return await GenerateYamlFiles(
                    path,
                    components, 
                    "component",
                    component => component.Guid,
                    component => ComponentTemplate.Generate(component)
                );
            }

            public async Task<(string path, int fileCount)> GenerateYamlFilesForSpecificSecurityRequirements(string path, List<Guid> libraryIds)
            {
                var securityRequirements = await _securityRequirementRepository.GetSecurityRequirementsByLibraryIdAsync(libraryIds);
                return await GenerateYamlFiles(
                    path,
                    securityRequirements,
                    "security-requirement",
                    securityRequirement => securityRequirement.Guid,
                    securityRequirement => SecurityRequirementTemplate.Generate(securityRequirement)
                );
            }

            public async Task<(string path, int fileCount)> GenerateYamlFilesForSpecificTestCases(string path, List<Guid> libraryIds)
            {
                var testCases = await _testcaseRepository.GetTestcasesByLibraryIdAsync(libraryIds);
                return await GenerateYamlFiles(
                    path,
                    testCases,
                    "testcase",
                    testCase => testCase.Guid,
                    testCase => TestCaseTemplate.Generate(testCase)
                );
            }

            public async Task<(string path, int fileCount)> GenerateYamlFilesForSpecificProperties(string path, List<Guid> propertyIds)
            {
                var properties = await _propertyRepository.GetPropertiesByLibraryIdAsync(propertyIds);
                return await GenerateYamlFiles(
                    path,
                    properties,
                    "property",
                    property => property.Guid,
                    property => PropertyTemplate.Generate(property)
                );
            }

            public async Task<(string path, int fileCount)> GenerateYamlFilesForSpecificPropertyOptions(string path)
            {
                var propertyOptions = await _propertyOptionRepository.GetAllPropertyOptionsAsync();
                return await GenerateYamlFiles(
                    path,
                    propertyOptions,
                    "property-option",
                    propertyOption => propertyOption.Id,
                    propertyOption => PropertyOptionTemplate.Generate(propertyOption)
                );
            }

            public async Task<(string path, int fileCount)> GenerateYamlFilesForComponentSecurityRequirementMappings(string path, List<Guid> libraryIds)
            {
                var mappings = await _componentLibraryMappingRepository.GetMappingsByLibraryIdAsync(libraryIds);
                return await GenerateYamlFiles(
                    path,
                    mappings,
                    "component-security-requirement-mapping",
                    mapping => mapping.Guid,
                    mapping => ComponentSecurityRequirementMappingTemplate.Generate(mapping)
                );
            }

            public async Task<(string path, int fileCount)> GenerateYamlFilesForComponentThreatMappings(string path, List<Guid> libraryIds)
            {
                var mappings = await _componentThreatMappingRepository.GetMappingsByLibraryIdAsync(libraryIds);
                return await GenerateYamlFiles(
                    path,
                    mappings,
                    "component-threat-mapping",
                    mapping => mapping.Guid,
                    mapping => ComponentThreatMappingTemplate.Generate(mapping)
                );
            }

            public async Task<(string path, int fileCount)> GenerateYamlFilesForComponentThreatSecurityRequirementMappings(string path, List<Guid> libraryIds)
            {
                var mappings = await _componentThreatSecurityRequirementMappingRepository.GetMappingsByLibraryIdAsync(libraryIds);
                return await GenerateYamlFiles(
                    path,
                    mappings,
                    "component-threat-security-requirement-mapping",
                    mapping => mapping.Guid,
                    mapping => ComponentThreatSecurityRequirementMappingTemplate.Generate(mapping)
                );
            }

            public async Task<(string path, int fileCount)> GenerateYamlFilesForThreatSecurityRequirementMappings(string path, List<Guid> libraryIds)
            {
                var mappings = await _threatSecurityRequirementMappingRepository.GetMappingsByLibraryIdAsync(libraryIds);
                return await GenerateYamlFiles(
                    path,
                    mappings,
                    "threat-security-requirement-mapping",
                    mapping => mapping.Guid,
                    mapping => ThreatSecurityRequirementMappingTemplate.Generate(mapping)
                );
            }

            public async Task<(string path, int fileCount)> GenerateYamlFilesForComponentPropertyMappings(string path, List<Guid> libraryIds)
            {
                var mappings = await _componentPropertyMappingRepository.GetMappingsByLibraryIdAsync(libraryIds);
                return await GenerateYamlFiles(
                    path,
                    mappings,
                    "component-property-mapping",
                    mapping => mapping.Guid,
                    mapping => ComponentPropertyMappingTemplate.Generate(mapping)
                );
            }

            public async Task<(string path, int fileCount)> GenerateYamlFilesForComponentPropertyOptionMappings(string path, List<Guid> libraryIds)
            {
                var mappings = await _componentPropertyOptionMappingRepository.GetMappingsByLibraryGuidAsync(libraryIds.FirstOrDefault());
                return await GenerateYamlFiles(
                    path,
                    mappings,
                    "component-property-option-mapping",
                    mapping => mapping.Guid,
                    mapping => ComponentPropertyOptionMappingTemplate.Generate(mapping)
                );
            }

            public async Task<(string path, int fileCount)> GenerateYamlFilesForComponentPropertyOptionThreatMappings(string path, List<Guid> libraryIds)
            {
                var mappings = await _componentPropertyOptionThreatMappingRepository.GetMappingsByLibraryGuidAsync(libraryIds);
                return await GenerateYamlFiles(
                    path,
                    mappings,
                    "component-property-option-threat-mapping",
                    mapping => mapping.Guid,
                    mapping => ComponentPropertyOptionThreatMappingTemplate.Generate(mapping)
                );
            }

            public async Task<(string path, int fileCount)> GenerateYamlFilesForComponentPropertyOptionThreatSecurityRequirementMappings(string path, List<Guid> libraryIds)
            {
                var mappings = await _componentPropertyOptionThreatSecurityRequirementMappingRepository.GetMappingsByLibraryGuidAsync(libraryIds);
                return await GenerateYamlFiles(
                    path,
                    mappings,
                    "component-property-option-threat-security-requirement-mapping",
                    mapping => mapping.Guid,
                    mapping => ComponentPropertyOptionThreatSecurityRequirementMappingTemplate.Generate(mapping)
                );
            }
        }
    }
