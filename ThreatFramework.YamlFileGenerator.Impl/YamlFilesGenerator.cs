using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ThreatFramework.Infrastructure.Interfaces;
using ThreatFramework.Infrastructure.Interfaces.Repositories;
using ThreatFramework.YamlFileGenerator.Contract;
using ThreatFramework.YamlFileGenerator.Impl.Templates;

namespace ThreatFramework.YamlFileGenerator.Impl
{
    public class YamlFilesGenerator : IYamlFileGenerator
    {
        private readonly IThreatRepository _threatRepository;
        private readonly IComponentRepository _componentRepository;
        private readonly ILibraryRepository _libraryRepository;
        private readonly ISecurityRequirementRepository _securityRequirementRepository;
        private readonly IPropertyRepository _propertyRepository;
        private readonly IPropertyOptionRepository _propertyOptionRepository;
        private readonly ITestcaseRepository _testcaseRepository;
        private readonly IIndexService _indexService;

        public YamlFilesGenerator(
            IThreatRepository threatRepository,
            IComponentRepository componentRepository,
            ILibraryRepository libraryRepository,
            ISecurityRequirementRepository securityRequirementRepository,
            IPropertyRepository propertyRepository,
            IPropertyOptionRepository propertyOptionRepository,
            ITestcaseRepository testcaseRepository,
            IIndexService indexService)
        {
            _threatRepository = threatRepository ?? throw new ArgumentNullException(nameof(threatRepository));
            _componentRepository = componentRepository ?? throw new ArgumentNullException(nameof(componentRepository));
            _libraryRepository = libraryRepository ?? throw new ArgumentNullException(nameof(libraryRepository));
            _securityRequirementRepository = securityRequirementRepository ?? throw new ArgumentNullException(nameof(securityRequirementRepository));
            _propertyRepository = propertyRepository ?? throw new ArgumentNullException(nameof(propertyRepository));
            _propertyOptionRepository = propertyOptionRepository ?? throw new ArgumentNullException(nameof(propertyOptionRepository));
            _testcaseRepository = testcaseRepository ?? throw new ArgumentNullException(nameof(testcaseRepository));
            _indexService = indexService ?? throw new ArgumentNullException(nameof(indexService));
        }

        public async Task<string> GenerateYamlFilesForComponents(string path)
        {
            var components = await _componentRepository.GetAllComponentsAsync();
            return await GenerateYamlFiles(
                path,
                components,
                "Component",
                c => c.Guid,
                ComponentYamlTemplate.GenerateComponentYaml,
                "components"
            );
        }

        public async Task<string> GenerateYamlFilesForThreats(string path)
        {
            var threats = await _threatRepository.GetAllThreatsAsync();
            return await GenerateYamlFiles(
                path,
                threats,
                "Threat",
                t => t.Guid,
                ThreatYamlTemplate.GenerateThreatYaml,
                "threats"
            );
        }

        public async Task<string> GenerateYamlFilesForLibraries(string path)
        {
            var libraries = await _libraryRepository.GetAllLibrariesAsync();
            return await GenerateYamlFiles(
                path,
                libraries,
                "Library",
                l => l.Guid,
                LibraryYamlTemplate.GenerateLibraryYaml,
                "libraries"
            );
        }

        public async Task<string> GenerateYamlFilesForSecurityRequirements(string path)
        {
            var securityRequirements = await _securityRequirementRepository.GetAllSecurityRequirementsAsync();
            return await GenerateYamlFiles(
                path,
                securityRequirements,
                "SecurityRequirement",
                sr => sr.Guid,
                SecurityRequirementYamlTemplate.GenerateSecurityRequirementYaml,
                "security requirements"
            );
        }

        public async Task<string> GenerateYamlFilesForProperties(string path)
        {
            var properties = await _propertyRepository.GetAllPropertiesAsync();
            return await GenerateYamlFiles(
                path,
                properties,
                "Property",
                p => p.Guid,
                PropertyYamlTemplate.GeneratePropertyYaml,
                "properties"
            );
        }

        public async Task<string> GenerateYamlFilesForPropertyOptions(string path)
        {
            var propertyOptions = await _propertyOptionRepository.GetAllPropertyOptionsAsync();
            return await GenerateYamlFiles(
                path,
                propertyOptions,
                "PropertyOption",
                po => po.Guid.Value,
                PropertyOptionYamlTemplate.GeneratePropertyOptionYaml,
                "property options"
            );
        }

        public async Task<string> GenerateYamlFilesForTestCases(string path)
        {
            var testCases = await _testcaseRepository.GetAllTestcasesAsync();
            return await GenerateYamlFiles(
                path,
                testCases,
                "TestCase",
                tc => tc.Guid,
                TestCaseYamlTemplate.GenerateTestCaseYaml,
                "test cases"
            );
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

        private async Task<string> GenerateYamlFiles<T>(
            string path,
            IEnumerable<T> items,
            string kind,
            Func<T, Guid> guidSelector,
            Func<T, string> yamlGenerator,
            string itemTypeName)
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

            return $"Generated {fileCount} YAML files for {itemTypeName} in '{path}'";
        }
    }   
}
