using System.Runtime.CompilerServices;
using ThreatFramework.Core.CoreEntities;
using ThreatFramework.Drift.Contract.CoreEntityDriftService;
using ThreatFramework.Infra.Contract.YamlRepository.CoreEntity;
using ThreatModeler.TF.Core.Global;

namespace ThreatFramework.Drift.Impl.CoreEntityDriftService
{
    public sealed class YamlReaderRouter : IYamlReaderRouter
    {
        private readonly IYamlThreatReader _threatReader;
        private readonly IYamlComponentReader _componentReader;
        private readonly IYamlTestcaseReader _testcaseReader;
        private readonly IYamlPropertyReader _propertyReader;
        private readonly IYamlPropertyOptionReader _propertyOptionReader;
        private readonly IYamlLibraryReader _libraryReader;
        private readonly IYamlSecurityRequirementReader _securityRequirementReader;

        public YamlReaderRouter(
            IYamlThreatReader threatReader,
            IYamlComponentReader componentReader,
            IYamlTestcaseReader testcaseReader,
            IYamlPropertyReader propertyReader,
            IYamlPropertyOptionReader propertyOptionReader,
            IYamlLibraryReader libraryReader,
            IYamlSecurityRequirementReader securityRequirementReader)
        {
            _threatReader = threatReader;
            _componentReader = componentReader;
            _testcaseReader = testcaseReader;
            _propertyReader = propertyReader;
            _propertyOptionReader = propertyOptionReader;
            _libraryReader = libraryReader;
            _securityRequirementReader = securityRequirementReader;
        }

        public Task<IEnumerable<Threat>> ReadThreatsAsync(IEnumerable<string> filePaths)
            => _threatReader.GetThreatsFromFilesAsync(filePaths);

        public Task<IEnumerable<Component>> ReadComponentsAsync(IEnumerable<string> filePaths)
            => _componentReader.GetComponentsFromFilesAsync(filePaths);

        public Task<IEnumerable<SecurityRequirement>> ReadSecurityRequirementsAsync(IEnumerable<string> filePaths)
            => _securityRequirementReader.GetSecurityRequirementsFromFilesAsync(filePaths);// add reader if/when available

        public Task<IEnumerable<TestCase>> ReadTestCasesAsync(IEnumerable<string> filePaths)
            => _testcaseReader.GetTestCasesFromFilesAsync(filePaths);

        public Task<IEnumerable<Property>> ReadPropertiesAsync(IEnumerable<string> filePaths)
            => _propertyReader.GetPropertiesFromFilesAsync(filePaths);

        public Task<IEnumerable<PropertyOption>> ReadPropertyOptionsAsync(IEnumerable<string> filePaths)
            => _propertyOptionReader.GetPropertyOption(filePaths);

        public Task<IEnumerable<Library>> ReadLibrariesAsync(IEnumerable<string> filePaths)
            => _libraryReader.GetLibrariesFromFilesAsync(filePaths);
    }
}
