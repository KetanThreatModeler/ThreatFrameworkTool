using ThreatFramework.Core.CoreEntities;
using ThreatFramework.Drift.Contract;
using ThreatFramework.Infra.Contract.YamlRepository.CoreEntity;

namespace ThreatFramework.Drift.Impl
{
    public sealed class YamlReaderRouter : IYamlReaderRouter
    {
        private readonly IYamlThreatReader _threatReader;
        private readonly IYamlComponentReader _componentReader;
        private readonly IYamlTestcaseReader _testcaseReader;
        private readonly IYamlPropertyReader _propertyReader;
        private readonly IYamlPropertyOptionReader _propertyOptionReader;
        private readonly IYamlLibraryReader _libraryReader;

        public YamlReaderRouter(
            IYamlThreatReader threatReader,
            IYamlComponentReader componentReader,
            IYamlTestcaseReader testcaseReader,
            IYamlPropertyReader propertyReader,
            IYamlPropertyOptionReader propertyOptionReader,
            IYamlLibraryReader libraryReader)
        {
            _threatReader = threatReader;
            _componentReader = componentReader;
            _testcaseReader = testcaseReader;
            _propertyReader = propertyReader;
            _propertyOptionReader = propertyOptionReader;
            _libraryReader = libraryReader;
        }

        public Task<IEnumerable<Threat>> ReadThreatsAsync(IEnumerable<string> filePaths, CancellationToken ct)
            => _threatReader.GetThreatsFromFilesAsync(filePaths);

        public Task<IEnumerable<Component>> ReadComponentsAsync(IEnumerable<string> filePaths, CancellationToken ct)
            => _componentReader.GetComponentsFromFilesAsync(filePaths);

        public Task<IEnumerable<SecurityRequirement>> ReadSecurityRequirementsAsync(IEnumerable<string> filePaths, CancellationToken ct)
            => Task.FromResult<IEnumerable<SecurityRequirement>>(Array.Empty<SecurityRequirement>()); // add reader if/when available

        public Task<IEnumerable<TestCase>> ReadTestCasesAsync(IEnumerable<string> filePaths, CancellationToken ct)
            => _testcaseReader.GetTestCasesFromFilesAsync(filePaths);

        public Task<IEnumerable<Property>> ReadPropertiesAsync(IEnumerable<string> filePaths, CancellationToken ct)
            => _propertyReader.GetPropertiesFromFilesAsync(filePaths);

        public Task<IEnumerable<PropertyOption>> ReadPropertyOptionsAsync(IEnumerable<string> filePaths, CancellationToken ct)
            => _propertyOptionReader.GetPropertyOption(filePaths);

        public Task<IEnumerable<Library>> ReadLibrariesAsync(IEnumerable<string> filePaths, CancellationToken ct)
            => _libraryReader.GetLibrariesFromFilesAsync(filePaths);
    }
}
