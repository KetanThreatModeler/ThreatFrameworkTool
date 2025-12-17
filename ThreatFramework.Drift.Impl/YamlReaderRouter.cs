using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThreatFramework.Infra.Contract.YamlRepository;
using ThreatFramework.Infra.Contract.YamlRepository.CoreEntity;
using ThreatModeler.TF.Core.Model.AssistRules;
using ThreatModeler.TF.Core.Model.CoreEntities;
using ThreatModeler.TF.Core.Model.Global;
using ThreatModeler.TF.Drift.Contract;
using ThreatModeler.TF.Infra.Contract.YamlRepository.AssistRules;
using ThreatModeler.TF.Infra.Contract.YamlRepository.Global;
using ThreatModeler.TF.Infra.Contract.YamlRepository.Mappings;

namespace ThreatModeler.TF.Drift.Implemenetation
{
    public sealed class YamlReaderRouter : IYamlReaderRouter
    {
        private readonly IYamlThreatReader _threatReader;
        private readonly IYamlComponentReader _componentReader;
        private readonly IYamlTestcaseReader _testcaseReader;
        private readonly IYamlPropertyReader _propertyReader;
        private readonly IYamlLibraryReader _libraryReader;
        private readonly IYamlSecurityRequirementReader _securityRequirementReader;
        private readonly IYamlComponentPropertyReader _yamlComponentPropertyReader;
        private readonly IYamlComponentPropertyOptionReader _yamlComponentPropertyOption;
        private readonly IYamlComponentPropertyOptionThreatReader _componentPropertyOptionThreatReader;
        private readonly IYamlCpoThreatSrReader _yamlCpoThreatSrReader;
        private readonly IYamlComponentThreatReader _yamlComponentThreatReader;
        private readonly IYamlComponentThreatSRReader _yamlThreatSecurityRequirementReader;
        private readonly IYamlComponentSRReader _yamlComponentSRReader;
        private readonly IYamlThreatSrReader _yamlThreatSrReader;
        private readonly IYamlPropertyTypeReader _yamlPropertyTypeReader;
        private readonly IYamlComponentTypeReader _yamlComponentTypeReader;
        private readonly IYamlPropertyOptionReader _propertyOptionReader;
        private readonly IYamlRelationshipReader _yamlRelationshipReader;
        private readonly IYamlResourceTypesValueReader _yamlResourceTypesValueReader;
        private readonly IYamlResourceTypesValueRelationshipReader _yamlResourceTypesValueRelationReader;

        public YamlReaderRouter(
            IYamlThreatReader threatReader,
            IYamlComponentReader componentReader,
            IYamlTestcaseReader testcaseReader,
            IYamlPropertyReader propertyReader,
            IYamlPropertyOptionReader propertyOptionReader,
            IYamlLibraryReader libraryReader,
            IYamlSecurityRequirementReader securityRequirementReader,
            IYamlComponentPropertyReader yamlComponentPropertyReader,
            IYamlComponentPropertyOptionReader yamlComponentPropertyOption,
            IYamlCpoThreatSrReader yamlCpoThreatSrReader,
            IYamlComponentThreatReader yamlComponentThreatReader,
            IYamlComponentThreatSRReader yamlThreatSecurityRequirementReader,
            IYamlComponentSRReader yamlComponentSRReader,
            IYamlThreatSrReader yamlThreatSrReader,
            IYamlPropertyTypeReader yamlPropertyTypeReader,
            IYamlComponentTypeReader yamlComponentTypeReader,
            IYamlRelationshipReader yamlRelationshipReader,
            IYamlResourceTypesValueReader yamlResourceTypesValueReader,
            IYamlResourceTypesValueRelationshipReader yamlResourceTypesValueRelationReader)
        {
            _threatReader = threatReader;
            _componentReader = componentReader;
            _testcaseReader = testcaseReader;
            _propertyReader = propertyReader;
            _propertyOptionReader = propertyOptionReader;
            _libraryReader = libraryReader;
            _securityRequirementReader = securityRequirementReader;
            _yamlComponentPropertyReader = yamlComponentPropertyReader;
            _yamlComponentPropertyOption = yamlComponentPropertyOption;
            _yamlCpoThreatSrReader = yamlCpoThreatSrReader;
            _yamlComponentThreatReader = yamlComponentThreatReader;
            _yamlThreatSecurityRequirementReader = yamlThreatSecurityRequirementReader;
            _yamlComponentSRReader = yamlComponentSRReader;
            _yamlThreatSrReader = yamlThreatSrReader;
            _yamlPropertyTypeReader = yamlPropertyTypeReader;
            _yamlComponentTypeReader = yamlComponentTypeReader;
            _yamlRelationshipReader = yamlRelationshipReader;
            _yamlResourceTypesValueReader = yamlResourceTypesValueReader;
            _yamlResourceTypesValueRelationReader = yamlResourceTypesValueRelationReader;
        }

        // -------- multi-file --------

        public async Task<IEnumerable<Threat>> ReadThreatsAsync(IEnumerable<string> filePaths)
            => await _threatReader.GetThreatsFromFilesAsync(filePaths);

        public async Task<IEnumerable<Component>> ReadComponentsAsync(IEnumerable<string> filePaths)
            => await _componentReader.GetComponentsFromFilesAsync(filePaths);

        public async Task<IEnumerable<SecurityRequirement>> ReadSecurityRequirementsAsync(IEnumerable<string> filePaths)
            => await _securityRequirementReader.GetSecurityRequirementsFromFilesAsync(filePaths);

        public async Task<IEnumerable<TestCase>> ReadTestCasesAsync(IEnumerable<string> filePaths)
            => await _testcaseReader.GetTestCasesFromFilesAsync(filePaths);

        public async Task<IEnumerable<Property>> ReadPropertiesAsync(IEnumerable<string> filePaths)
            => await _propertyReader.GetPropertiesFromFilesAsync(filePaths);

        public async Task<IEnumerable<PropertyOption>> ReadPropertyOptionsAsync(IEnumerable<string> filePaths)
            => await _propertyOptionReader.GetPropertyOption(filePaths);

        public async Task<IEnumerable<Library>> ReadLibrariesAsync(IEnumerable<string> filePaths)
            => await _libraryReader.GetLibrariesFromFilesAsync(filePaths);

        public async Task<IEnumerable<PropertyType>> ReadPropertyTypesAsync(IEnumerable<string> filePaths)
            => await _yamlPropertyTypeReader.GetPropertyTypesFromFilesAsync(filePaths);

        public async Task<IEnumerable<ComponentType>> ReadComponentTypeAsync(IEnumerable<string> filePaths)
            => await _yamlComponentTypeReader.GetComponentTypesFromFilesAsync(filePaths);

        // -------- single-file --------

        public async Task<Threat> ReadThreatAsync(string filePath)
            => await _threatReader.GetThreatFromFileAsync(filePath);

        public async Task<Component> ReadComponentAsync(string filePath)
        {
            // Component reader doesn’t expose single-file API, so we wrap multi-file
            var components = await _componentReader
                .GetComponentsFromFilesAsync(new[] { filePath })
                .ConfigureAwait(false);

            return components.FirstOrDefault();
        }

        public async Task<SecurityRequirement> ReadSecurityRequirementAsync(string filePath)
            => await _securityRequirementReader.GetSecurityRequirementFromFileAsync(filePath);

        public async Task<TestCase> ReadTestCaseAsync(string filePath)
            => await _testcaseReader.GetTestCaseFromFileAsync(filePath);

        public async Task<Property> ReadPropertyAsync(string filePath)
            => await _propertyReader.GetPropertyFromFileAsync(filePath);

        public async Task<PropertyOption> ReadPropertyOptionAsync(string filePath)
            => await _propertyOptionReader.GetPropertyOptionFromFileAsync(filePath);

        public async Task<Library> ReadLibraryAsync(string filePath)
        {
            // Library reader only supports multi-file → wrap it
            var libs = await _libraryReader
                .GetLibrariesFromFilesAsync(new[] { filePath })
                .ConfigureAwait(false);

            return libs.FirstOrDefault();
        }

        public async Task<PropertyType> ReadPropertyTypeAsync(string filePath)
            => await _yamlPropertyTypeReader.GetPropertyTypeFromFileAsync(filePath);

        public async Task<ComponentType> ReadComponentTypeAsync(string filePath)
            => await _yamlComponentTypeReader.GetComponentTypeFromFileAsync(filePath);

        // -------- AssistRules : single-file --------

        public async Task<Relationship> ReadRelationshipAsync(string filePath)
            => await _yamlRelationshipReader.ReadRelationshipAsync(filePath);

        public async Task<ResourceTypeValues> ReadResourceTypeValuesAsync(string filePath)
            => await _yamlResourceTypesValueReader.GetResourceTypeValue(filePath);

        public async Task<ResourceTypeValueRelationship> ReadResourceTypeValueRelationshipAsync(string filePath)
            => await _yamlResourceTypesValueRelationReader.GetResourceTypeValueRelationship(filePath);


        // -------- AssistRules : multi-file --------

        public async Task<IEnumerable<Relationship>> ReadRelationshipsAsync(IEnumerable<string> filePaths)
            => await _yamlRelationshipReader.ReadRelationshipsAsync(filePaths);

        public async Task<IEnumerable<ResourceTypeValues>> ReadResourceTypeValuesAsync(IEnumerable<string> filePaths)
            => await _yamlResourceTypesValueReader.GetResourceTypeValues(filePaths);

        public async Task<IEnumerable<ResourceTypeValueRelationship>> ReadResourceTypeValueRelationsAsync(IEnumerable<string> filePaths)
            => await _yamlResourceTypesValueRelationReader.GetResourceTypeValueRelationships(filePaths);

    }
}
