using Microsoft.Extensions.Logging;
using ThreatFramework.Infra.Contract.Repository;
using ThreatFramework.YamlFileGenerator.Contract;
using ThreatFramework.YamlFileGenerator.Impl.Templates.ComponentMapping;
using ThreatFramework.YamlFileGenerator.Impl.Templates.PropertyMapping;
using ThreatModeler.TF.Core.Model.AssistRules;
using ThreatModeler.TF.Core.Model.ComponentMapping;
using ThreatModeler.TF.Core.Model.CoreEntities;
using ThreatModeler.TF.Core.Model.Global;
using ThreatModeler.TF.Core.Model.PropertyMapping;
using ThreatModeler.TF.Core.Model.ThreatMapping;
using ThreatModeler.TF.Git.Contract.Common;
using ThreatModeler.TF.Infra.Contract.AssistRuleIndex.Common;
using ThreatModeler.TF.Infra.Contract.AssistRuleIndex.TRC;
using ThreatModeler.TF.Infra.Contract.Index.Common;
using ThreatModeler.TF.Infra.Contract.Index.TRC;
using ThreatModeler.TF.Infra.Contract.Repository.AssistRules;
using ThreatModeler.TF.Infra.Contract.Repository.CoreEntities;
using ThreatModeler.TF.Infra.Contract.Repository.Global;
using ThreatModeler.TF.Infra.Contract.Repository.ThreatMapping;
using ThreatModeler.TF.YamlFileGenerator.Contract;
using ThreatModeler.TF.YamlFileGenerator.Implementation.Templates.AssistRule;
using ThreatModeler.TF.YamlFileGenerator.Implementation.Templates.CoreEntitites;
using ThreatModeler.TF.YamlFileGenerator.Implementation.Templates.Global;

namespace ThreatFramework.YamlFileGenerator.Impl
{
    public class UtilsForTRCYamlFilesGenerator : ITRCYamlFileGenerator
    {
        private const string FileNameSeperator = "_";

        private readonly ILogger<UtilsForTRCYamlFilesGenerator> _logger;
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

        private readonly ITRCGuidIndexService _indexService;
        private readonly ITRCAssistRuleIndexService _assistRuleIndexService;

        private readonly IRelationshipRepository _relationshipRepository;
        private readonly IResourceTypeValuesRepository _resourceTypeValuesRepository;
        private readonly IResourceTypeValueRelationshipRepository _resourceTypeValueRelationshipRepository;

        public UtilsForTRCYamlFilesGenerator(
            ILogger<UtilsForTRCYamlFilesGenerator> logger,
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
            ITRCGuidIndexService indexService,
            ITRCAssistRuleIndexService assistRuleIndexService,
            IRelationshipRepository relationshipRepository,
            IResourceTypeValuesRepository resourceTypeValuesRepository,
            IResourceTypeValueRelationshipRepository resourceTypeValueRelationshipRepository)
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
            _assistRuleIndexService = assistRuleIndexService ?? throw new ArgumentNullException(nameof(assistRuleIndexService));

            _relationshipRepository = relationshipRepository ?? throw new ArgumentNullException(nameof(relationshipRepository));
            _resourceTypeValuesRepository = resourceTypeValuesRepository ?? throw new ArgumentNullException(nameof(resourceTypeValuesRepository));
            _resourceTypeValueRelationshipRepository = resourceTypeValueRelationshipRepository ?? throw new ArgumentNullException(nameof(resourceTypeValueRelationshipRepository));

            _logger.LogInformation("YamlFilesGenerator initialized successfully");
        }

        // ----------------------------
        // Small DRY helpers (no delegates)
        // ----------------------------

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

        private static string CombineFolder(string root, params string[] parts)
        {
            var folder = root;
            foreach (var p in parts)
            {
                folder = Path.Combine(folder, p);
            }

            EnsureDirectoryExists(folder);
            return folder;
        }

        private async Task<string> GetLibraryFolderAsync(string rootPath, Guid libraryGuid)
        {
            var libraryId = await _indexService.GetIntAsync(libraryGuid);
            var folder = Path.Combine(rootPath, libraryId.ToString());
            EnsureDirectoryExists(folder);
            return folder;
        }

        private async Task WriteYamlAsync(string folder, string fileName, string yaml)
        {
            EnsureDirectoryExists(folder);
            await File.WriteAllTextAsync(Path.Combine(folder, fileName), yaml);
        }

        private static string FileNameFromIds(params int[] ids)
            => string.Join(FileNameSeperator, ids.Select(i => i.ToString())) + ".yaml";

        private async Task<int> GetAssistRelationshipIdOrThrowAsync(Guid relationshipGuid)
        {
            var id = await _assistRuleIndexService.GetIdByRelationshipGuidAsync(relationshipGuid);
            if (id < 0)
            {
                _logger.LogError("AssistRuleIndex ID not found for RelationshipGuid={Guid}", relationshipGuid);
                throw new InvalidOperationException($"AssistRuleIndex ID not found for RelationshipGuid: {relationshipGuid}");
            }
            return id;
        }

        private async Task<int> GetAssistResourceTypeValueIdOrThrowAsync(string resourceTypeValue)
        {
            var id = await _assistRuleIndexService.GetIdByResourceTypeValueAsync(resourceTypeValue);
            if (id < 0)
            {
                _logger.LogError("AssistRuleIndex ID not found for ResourceTypeValue={Value}", resourceTypeValue);
                throw new InvalidOperationException($"AssistRuleIndex ID not found for ResourceTypeValue: {resourceTypeValue}");
            }
            return id;
        }

        // ----------------------------
        // Generators
        // ----------------------------

        public async Task<(string path, int fileCount)> GenerateYamlFilesForSpecificLibraries(string path, List<Guid> libraryIds)
        {
            ValidatePath(path);
            EnsureDirectoryExists(path);

            _logger.LogInformation("Generating YAML files for {LibraryCount} libraries", libraryIds?.Count ?? 0);
            if (libraryIds == null || libraryIds.Count == 0) return (path, 0);

            var libraries = await _libraryRepository.GetLibrariesByGuidsAsync(libraryIds);
            var list = libraries?.ToList() ?? new List<Library>(); // adjust if your Library type differs

            var count = 0;

            foreach (var library in list)
            {
                var libraryFolder = await GetLibraryFolderAsync(path, library.Guid);

                var libId = await _indexService.GetIntAsync(library.Guid);
                await WriteYamlAsync(libraryFolder, $"{libId}.yaml", LibraryTemplate.Generate(library));
                count++;
            }

            _logger.LogInformation("Generated {FileCount} Library YAML files in path: {Path}", count, path);
            return (path, count);
        }

        public async Task<(string path, int fileCount)> GenerateYamlFilesForSpecificThreats(string path, List<Guid> libraryIds)
        {
            ValidatePath(path);
            EnsureDirectoryExists(path);

            _logger.LogInformation("Generating YAML files for threats from {LibraryCount} libraries", libraryIds?.Count ?? 0);
            if (libraryIds == null || libraryIds.Count == 0) return (path, 0);

            var threats = await _threatRepository.GetThreatsByLibraryIdAsync(libraryIds);
            var list = threats?.ToList() ?? new List<Threat>(); // adjust if needed

            if (!list.Any())
                return (path, 0);

            var count = 0;

            foreach (var group in list.GroupBy(t => t.LibraryGuid))
            {
                var libraryFolder = await GetLibraryFolderAsync(path, group.Key);
                var threatsFolder = CombineFolder(libraryFolder, FolderNames.Threats);

                foreach (var threat in group)
                {
                    var threatId = await _indexService.GetIntAsync(threat.Guid);
                    await WriteYamlAsync(threatsFolder, $"{threatId}.yaml", ThreatTemplate.Generate(threat));
                    count++;
                }
            }

            _logger.LogInformation("Generated {FileCount} Threat YAML files in path: {Path}", count, path);
            return (path, count);
        }

        public async Task<(string path, int fileCount)> GenerateYamlFilesForSpecificComponents(string path, List<Guid> libraryIds)
        {
            ValidatePath(path);
            EnsureDirectoryExists(path);

            _logger.LogInformation("Generating YAML files for components from {LibraryCount} libraries", libraryIds?.Count ?? 0);
            if (libraryIds == null || libraryIds.Count == 0) return (path, 0);

            var components = await _componentRepository.GetComponentsByLibraryIdAsync(libraryIds);
            var list = components?.ToList() ?? new List<Component>(); // adjust if needed

            if (!list.Any())
                return (path, 0);

            var count = 0;

            foreach (var group in list.GroupBy(c => c.LibraryGuid))
            {
                var libraryFolder = await GetLibraryFolderAsync(path, group.Key);
                var componentsFolder = CombineFolder(libraryFolder, FolderNames.Components);

                foreach (var component in group)
                {
                    var componentId = await _indexService.GetIntAsync(component.Guid);
                    await WriteYamlAsync(componentsFolder, $"{componentId}.yaml", ComponentTemplate.Generate(component));
                    count++;
                }
            }

            _logger.LogInformation("Generated {FileCount} Component YAML files in path: {Path}", count, path);
            return (path, count);
        }

        public async Task<(string path, int fileCount)> GenerateYamlFilesForSpecificSecurityRequirements(string path, List<Guid> libraryIds)
        {
            ValidatePath(path);
            EnsureDirectoryExists(path);

            _logger.LogInformation("Generating YAML files for security requirements from {LibraryCount} libraries", libraryIds?.Count ?? 0);
            if (libraryIds == null || libraryIds.Count == 0) return (path, 0);

            var securityRequirements = await _securityRequirementRepository.GetSecurityRequirementsByLibraryIdAsync(libraryIds);
            var list = securityRequirements?.ToList() ?? new List<SecurityRequirement>(); // adjust if needed

            if (!list.Any())
                return (path, 0);

            var count = 0;

            foreach (var group in list.GroupBy(sr => sr.LibraryId))
            {
                var libraryFolder = await GetLibraryFolderAsync(path, group.Key);
                var folder = CombineFolder(libraryFolder, FolderNames.SecurityRequirements);

                foreach (var sr in group)
                {
                    var srId = await _indexService.GetIntAsync(sr.Guid);
                    await WriteYamlAsync(folder, $"{srId}.yaml", SecurityRequirementTemplate.Generate(sr));
                    count++;
                }
            }

            _logger.LogInformation("Generated {FileCount} SecurityRequirement YAML files in path: {Path}", count, path);
            return (path, count);
        }

        public async Task<(string path, int fileCount)> GenerateYamlFilesForSpecificTestCases(string path, List<Guid> libraryIds)
        {
            ValidatePath(path);
            EnsureDirectoryExists(path);

            _logger.LogInformation("Generating YAML files for test cases from {LibraryCount} libraries", libraryIds?.Count ?? 0);
            if (libraryIds == null || libraryIds.Count == 0) return (path, 0);

            var testCases = await _testcaseRepository.GetTestcasesByLibraryIdAsync(libraryIds);
            var list = testCases?.ToList() ?? new List<TestCase>(); // adjust if needed

            if (!list.Any())
                return (path, 0);

            var count = 0;

            foreach (var group in list.GroupBy(tc => tc.LibraryId))
            {
                var libraryFolder = await GetLibraryFolderAsync(path, group.Key);
                var folder = CombineFolder(libraryFolder, FolderNames.TestCases);

                foreach (var tc in group)
                {
                    var tcId = await _indexService.GetIntAsync(tc.Guid);
                    await WriteYamlAsync(folder, $"{tcId}.yaml", TestCaseTemplate.Generate(tc));
                    count++;
                }
            }

            _logger.LogInformation("Generated {FileCount} Testcase YAML files in path: {Path}", count, path);
            return (path, count);
        }

        public async Task<(string path, int fileCount)> GenerateYamlFilesForSpecificProperties(string path, List<Guid> libraryIds)
        {
            ValidatePath(path);
            EnsureDirectoryExists(path);

            _logger.LogInformation("Generating YAML files for properties from {LibraryCount} libraries", libraryIds?.Count ?? 0);
            if (libraryIds == null || libraryIds.Count == 0) return (path, 0);

            var properties = await _propertyRepository.GetAllPropertiesAsync();
            var all = properties?.ToList() ?? new List<Property>(); // adjust if needed

            var list = all.Where(p => libraryIds.Contains(p.LibraryGuid)).ToList();
            if (!list.Any())
                return (path, 0);

            var count = 0;

            foreach (var group in list.GroupBy(p => p.LibraryGuid))
            {
                var libraryFolder = await GetLibraryFolderAsync(path, group.Key);
                var folder = CombineFolder(libraryFolder, FolderNames.Properties);

                foreach (var prop in group)
                {
                    var propId = await _indexService.GetIntAsync(prop.Guid);
                    await WriteYamlAsync(folder, $"{propId}.yaml", PropertyTemplate.Generate(prop));
                    count++;
                }
            }

            _logger.LogInformation("Generated {FileCount} Property YAML files in path: {Path}", count, path);
            return (path, count);
        }

        public async Task<(string path, int fileCount)> GenerateYamlFilesForPropertyOptions(string path)
        {
            ValidatePath(path);

            _logger.LogInformation("Generating YAML files for all property options");

            var folder = CombineFolder(path, "global", FolderNames.PropertyOptions);

            var propertyOptions = await _propertyOptionRepository.GetAllPropertyOptionsAsync();
            var list = propertyOptions?.ToList() ?? new List<PropertyOption>(); // adjust if needed

            var count = 0;

            foreach (var option in list)
            {
                var id = await _indexService.GetIntAsync(option.Guid);
                await WriteYamlAsync(folder, $"{id}.yaml", PropertyOptionTemplate.Generate(option));
                count++;
            }

            _logger.LogInformation("Generated {FileCount} PropertyOption YAML files in path: {Path}", count, folder);
            return (folder, count);
        }

        // ----------------------------
        // Mappings (fixed: always await ids)
        // ----------------------------

        public async Task<(string path, int fileCount)> GenerateYamlFilesForComponentSecurityRequirementMappings(string path, List<Guid> libraryIds)
        {
            ValidatePath(path);
            EnsureDirectoryExists(path);

            _logger.LogInformation("Generating YAML files for component-security requirement mappings from {LibraryCount} libraries", libraryIds?.Count ?? 0);
            if (libraryIds == null || libraryIds.Count == 0) return (path, 0);

            var mappings = await _componentLibraryMappingRepository.GetMappingsByLibraryIdAsync(libraryIds);
            var list = mappings?.ToList() ?? new List<ComponentSecurityRequirementMapping>();

            var count = 0;

            foreach (var m in list)
            {
                var componentId = await _indexService.GetIntAsync(m.ComponentGuid);
                var srId = await _indexService.GetIntAsync(m.SecurityRequirementGuid);

                var fileName = FileNameFromIds(componentId, srId);
                await WriteYamlAsync(path, fileName, CSRMappingTemplate.Generate(m));
                count++;
            }

            _logger.LogInformation("Generated {FileCount} YAML mapping files in path: {Path}", count, path);
            return (path, count);
        }

        public async Task<(string path, int fileCount)> GenerateYamlFilesForComponentThreatMappings(string path, List<Guid> libraryIds)
        {
            ValidatePath(path);
            EnsureDirectoryExists(path);

            _logger.LogInformation("Generating YAML files for component-threat mappings from {LibraryCount} libraries", libraryIds?.Count ?? 0);
            if (libraryIds == null || libraryIds.Count == 0) return (path, 0);

            var mappings = await _componentThreatMappingRepository.GetMappingsByLibraryIdAsync(libraryIds);
            var list = mappings?.ToList() ?? new List<ComponentThreatMapping>();

            var count = 0;

            foreach (var m in list)
            {
                var componentId = await _indexService.GetIntAsync(m.ComponentGuid);
                var threatId = await _indexService.GetIntAsync(m.ThreatGuid);

                var fileName = FileNameFromIds(componentId, threatId);
                await WriteYamlAsync(path, fileName, CTHMappingTemplate.Generate(m));
                count++;
            }

            _logger.LogInformation("Generated {FileCount} YAML mapping files in path: {Path}", count, path);
            return (path, count);
        }

        public async Task<(string path, int fileCount)> GenerateYamlFilesForComponentThreatSecurityRequirementMappings(string path, List<Guid> libraryIds)
        {
            ValidatePath(path);
            EnsureDirectoryExists(path);

            _logger.LogInformation("Generating YAML files for component-threat-security requirement mappings from {LibraryCount} libraries", libraryIds?.Count ?? 0);
            if (libraryIds == null || libraryIds.Count == 0) return (path, 0);

            var mappings = await _componentThreatSecurityRequirementMappingRepository.GetMappingsByLibraryIdAsync(libraryIds);
            var list = mappings?.ToList() ?? new List<ComponentThreatSecurityRequirementMapping>();

            var count = 0;

            foreach (var m in list)
            {
                var componentId = await _indexService.GetIntAsync(m.ComponentGuid);
                var threatId = await _indexService.GetIntAsync(m.ThreatGuid);
                var srId = await _indexService.GetIntAsync(m.SecurityRequirementGuid);

                var fileName = FileNameFromIds(componentId, threatId, srId);
                await WriteYamlAsync(path, fileName, CTSRMappingTemplate.Generate(m));
                count++;
            }

            _logger.LogInformation("Generated {FileCount} YAML mapping files in path: {Path}", count, path);
            return (path, count);
        }

        public async Task<(string path, int fileCount)> GenerateYamlFilesForThreatSecurityRequirementMappings(string path, List<Guid> libraryIds)
        {
            ValidatePath(path);
            EnsureDirectoryExists(path);

            _logger.LogInformation("Generating YAML files for threat-security requirement mappings from {LibraryCount} libraries", libraryIds?.Count ?? 0);
            if (libraryIds == null || libraryIds.Count == 0) return (path, 0);

            var mappings = await _threatSecurityRequirementMappingRepository.GetMappingsByLibraryIdAsync(libraryIds);
            var list = mappings?.ToList() ?? new List<ThreatSecurityRequirementMapping>();

            var count = 0;

            foreach (var m in list)
            {
                var threatId = await _indexService.GetIntAsync(m.ThreatGuid);
                var srId = await _indexService.GetIntAsync(m.SecurityRequirementGuid);

                var fileName = FileNameFromIds(threatId, srId);
                await WriteYamlAsync(path, fileName, TSRMappingTemplate.Generate(m));
                count++;
            }

            _logger.LogInformation("Generated {FileCount} YAML mapping files in path: {Path}", count, path);
            return (path, count);
        }

        public async Task<(string path, int fileCount)> GenerateYamlFilesForComponentPropertyMappings(string path, List<Guid> libraryIds)
        {
            ValidatePath(path);
            EnsureDirectoryExists(path);

            _logger.LogInformation("Generating YAML files for component-property mappings from {LibraryCount} libraries", libraryIds?.Count ?? 0);
            if (libraryIds == null || libraryIds.Count == 0) return (path, 0);

            var mappings = await _componentPropertyMappingRepository.GetMappingsByLibraryIdAsync(libraryIds);
            var list = mappings?.ToList() ?? new List<ComponentPropertyMapping>();

            var count = 0;

            foreach (var m in list)
            {
                var componentId = await _indexService.GetIntAsync(m.ComponentGuid);
                var propertyId = await _indexService.GetIntAsync(m.PropertyGuid);

                var fileName = FileNameFromIds(componentId, propertyId);
                await WriteYamlAsync(path, fileName, CPTemplate.Generate(m));
                count++;
            }

            _logger.LogInformation("Generated {FileCount} YAML mapping files in path: {Path}", count, path);
            return (path, count);
        }

        public async Task<(string path, int fileCount)> GenerateYamlFilesForComponentPropertyOptionMappings(string path, List<Guid> libraryIds)
        {
            ValidatePath(path);
            EnsureDirectoryExists(path);

            _logger.LogInformation("Generating YAML files for component-property-option mappings from {LibraryCount} libraries", libraryIds?.Count ?? 0);
            if (libraryIds == null || libraryIds.Count == 0) return (path, 0);

            var mappings = await _componentPropertyOptionMappingRepository.GetMappingsByLibraryGuidAsync(libraryIds);
            var list = mappings?.ToList() ?? new List<ComponentPropertyOptionMapping>();

            var count = 0;

            foreach (var m in list)
            {
                var componentId = await _indexService.GetIntAsync(m.ComponentGuid);
                var propertyId = await _indexService.GetIntAsync(m.PropertyGuid);
                var optionId = await _indexService.GetIntAsync(m.PropertyOptionGuid);

                var fileName = FileNameFromIds(componentId, propertyId, optionId);
                await WriteYamlAsync(path, fileName, CPOTemplate.Generate(m));
                count++;
            }

            _logger.LogInformation("Generated {FileCount} YAML mapping files in path: {Path}", count, path);
            return (path, count);
        }

        public async Task<(string path, int fileCount)> GenerateYamlFilesForComponentPropertyOptionThreatMappings(string path, List<Guid> libraryIds)
        {
            ValidatePath(path);
            EnsureDirectoryExists(path);

            _logger.LogInformation("Generating YAML files for component-property-option-threat mappings from {LibraryCount} libraries", libraryIds?.Count ?? 0);
            if (libraryIds == null || libraryIds.Count == 0) return (path, 0);

            var mappings = await _componentPropertyOptionThreatMappingRepository.GetMappingsByLibraryGuidAsync(libraryIds);
            var list = mappings?.ToList() ?? new List<ComponentPropertyOptionThreatMapping>();

            var count = 0;

            foreach (var m in list)
            {
                var componentId = await _indexService.GetIntAsync(m.ComponentGuid);
                var propertyId = await _indexService.GetIntAsync(m.PropertyGuid);
                var optionId = await _indexService.GetIntAsync(m.PropertyOptionGuid);
                var threatId = await _indexService.GetIntAsync(m.ThreatGuid);

                var fileName = FileNameFromIds(componentId, propertyId, optionId, threatId);
                await WriteYamlAsync(path, fileName, CPOThreatTemplate.Generate(m));
                count++;
            }

            _logger.LogInformation("Generated {FileCount} YAML mapping files in path: {Path}", count, path);
            return (path, count);
        }

        public async Task<(string path, int fileCount)> GenerateYamlFilesForComponentPropertyOptionThreatSecurityRequirementMappings(string path, List<Guid> libraryIds)
        {
            ValidatePath(path);
            EnsureDirectoryExists(path);

            _logger.LogInformation("Generating YAML files for component-property-option-threat-security requirement mappings from {LibraryCount} libraries", libraryIds?.Count ?? 0);
            if (libraryIds == null || libraryIds.Count == 0) return (path, 0);

            var mappings = await _componentPropertyOptionThreatSecurityRequirementMappingRepository.GetMappingsByLibraryGuidAsync(libraryIds);
            var list = mappings?.ToList() ?? new List<ComponentPropertyOptionThreatSecurityRequirementMapping>();

            var count = 0;

            foreach (var m in list)
            {
                var componentId = await _indexService.GetIntAsync(m.ComponentGuid);
                var propertyId = await _indexService.GetIntAsync(m.PropertyGuid);
                var optionId = await _indexService.GetIntAsync(m.PropertyOptionGuid);
                var threatId = await _indexService.GetIntAsync(m.ThreatGuid);
                var srId = await _indexService.GetIntAsync(m.SecurityRequirementGuid);

                var fileName = FileNameFromIds(componentId, propertyId, optionId, threatId, srId);
                await WriteYamlAsync(path, fileName, CPOTSRTemplate.Generate(m));
                count++;
            }

            _logger.LogInformation("Generated {FileCount} YAML mapping files in path: {Path}", count, path);
            return (path, count);
        }

        // ----------------------------
        // Global entities
        // ----------------------------

        public async Task<(string path, int fileCount)> GenerateYamlFilesForAllComponentTypes(string path)
        {
            ValidatePath(path);

            var folder = CombineFolder(path, "global", FolderNames.ComponentType);
            _logger.LogInformation("Generating YAML files for ComponentTypes");

            var componentTypes = await _componentTypeRepository.GetComponentTypesAsync();
            var list = componentTypes?.ToList() ?? new List<ComponentType>(); // adjust if needed

            var count = 0;

            foreach (var ct in list)
            {
                var id = await _indexService.GetIntAsync(ct.Guid);
                await WriteYamlAsync(folder, $"{id}.yaml", ComponentTypeTemplate.Generate(ct));
                count++;
            }

            _logger.LogInformation("Generated {FileCount} ComponentType YAML files in path: {Path}", count, folder);
            return (folder, count);
        }

        public async Task<(string path, int fileCount)> GenerateYamlFilesForPropertyTypes(string path)
        {
            ValidatePath(path);

            var folder = CombineFolder(path, "global", FolderNames.PropertyType);
            _logger.LogInformation("Generating YAML files for PropertyTypes");

            var propertyTypes = await _propertyTypeRepository.GetAllPropertyTypeAsync();
            var list = propertyTypes?.ToList() ?? new List<PropertyType>(); // adjust if needed

            var count = 0;

            foreach (var pt in list)
            {
                var id = await _indexService.GetIntAsync(pt.Guid);
                await WriteYamlAsync(folder, $"{id}.yaml", PropertyTypeTemplate.Generate(pt));
                count++;
            }

            _logger.LogInformation("Generated {FileCount} PropertyType YAML files in path: {Path}", count, folder);
            return (folder, count);
        }

        public async Task<(string path, int fileCount)> GenerateYamlFilesForRelationships(string path)
        {
            ValidatePath(path);

            _logger.LogInformation("Generating YAML files for Relationships");
            var folder = CombineFolder(path, "global", FolderNames.Relationships);

            var relationships = await _relationshipRepository.GetAllRelationshipsAsync();
            var list = relationships?.ToList() ?? new List<Relationship>();

            if (!list.Any())
            {
                _logger.LogWarning("No Relationships found. No YAML files generated.");
                return (folder, 0);
            }

            var count = 0;

            foreach (var rel in list)
            {
                var id = await GetAssistRelationshipIdOrThrowAsync(rel.Guid);
                await WriteYamlAsync(folder, $"REL{id}.yaml", RelationshipTemplate.Generate(rel));
                count++;
            }

            _logger.LogInformation("Generated {FileCount} Relationship YAML files in path: {Path}", count, folder);
            return (folder, count);
        }

        public async Task<(string path, int fileCount)> GenerateYamlFilesForResourceTypeValues(string path, List<Guid> libraryIds)
        {
            ValidatePath(path);
            EnsureDirectoryExists(path);

            _logger.LogInformation("Generating YAML files for ResourceTypeValues for {LibraryCount} libraries", libraryIds?.Count ?? 0);
            if (libraryIds == null || libraryIds.Count == 0) return (path, 0);

            var values = await _resourceTypeValuesRepository.GetByLibraryIdsAsync(libraryIds);
            var list = values?.ToList() ?? new List<ResourceTypeValues>();

            if (!list.Any())
            {
                _logger.LogWarning("No ResourceTypeValues found for provided libraries. No YAML files generated.");
                return (path, 0);
            }

            var count = 0;

            foreach (var v in list)
            {
                var assistId = await GetAssistResourceTypeValueIdOrThrowAsync(v.ResourceTypeValue);

                var libraryFolder = await GetLibraryFolderAsync(path, v.LibraryId);
                var rtvFolder = CombineFolder(libraryFolder, FolderNames.ResourceTypeValues);

                var componentId = await _indexService.GetIntForClientIndexGenerationAsync(v.ComponentGuid);
                if(componentId == 0)
                {
                    _logger.LogWarning("Component ID not found for ResourceTypeValue={Value} with ComponentGuid={Guid}", v.ResourceTypeValue, v.ComponentGuid);
                    continue;
                }
                var fileName = $"rtv{assistId}{FileNameSeperator}{componentId}.yaml";
                await WriteYamlAsync(rtvFolder, fileName, ResourceTypeValuesTemplate.Generate(v));
                count++;
            }

            _logger.LogInformation("Generated {FileCount} ResourceTypeValues YAML files under root path: {Path}", count, path);
            return (path, count);
        }

        public async Task<(string path, int fileCount)> GenerateYamlFilesForResourceTypeValueRelationships(string path, List<Guid> libraryIds)
        {
            ValidatePath(path);
            EnsureDirectoryExists(path);

            _logger.LogInformation("Generating YAML files for ResourceTypeValueRelationships for {LibraryCount} libraries", libraryIds?.Count ?? 0);
            if (libraryIds == null || libraryIds.Count == 0) return (path, 0);

            var count = 0;

            foreach (var libraryGuid in libraryIds)
            {
                var items = await _resourceTypeValueRelationshipRepository.GetByLibraryGuidsAsync(new List<Guid> { libraryGuid });
                var list = items?.ToList() ?? new List<ResourceTypeValueRelationship>();

                if (!list.Any())
                    continue;

                var libraryFolder = await GetLibraryFolderAsync(path, libraryGuid);
                var relFolder = CombineFolder(libraryFolder, FolderNames.ResourceValueTypeRelationship);

                foreach (var item in list)
                {
                    var sourceId = await _assistRuleIndexService.GetIdOrDefaultByResourceTypeValueAsync(item.SourceResourceTypeValue);
                    if(sourceId == 0)
                    {
                        _logger.LogWarning("Source ResourceTypeValue ID not found for ResourceTypeValueRelationship with SourceResourceTypeValue={Value}", item.SourceResourceTypeValue);
                        continue;
                    }
                    var relId = await GetAssistRelationshipIdOrThrowAsync(item.RelationshipGuid);
                    var targetId = await _assistRuleIndexService.GetIdOrDefaultByResourceTypeValueAsync(item.TargetResourceTypeValue);
                    if(targetId == 0)
                    {
                        _logger.LogWarning("Target ResourceTypeValue ID not found for ResourceTypeValueRelationship with TargetResourceTypeValue={Value}", item.TargetResourceTypeValue);
                        continue;
                    }

                    var fileName = $"rtv{sourceId}{FileNameSeperator}{relId}{FileNameSeperator}rtv{targetId}.yaml";
                    await WriteYamlAsync(relFolder, fileName, ResourceTypeValueRelationshipTemplate.Generate(item));
                    count++;
                }
            }

            _logger.LogInformation("Generated {FileCount} ResourceTypeValueRelationships YAML files under root path: {Path}", count, path);
            return (path, count);
        }
    }
}
