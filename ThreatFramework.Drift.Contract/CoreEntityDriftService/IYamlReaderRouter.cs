using System.Collections.Generic;
using System.Threading.Tasks;
using ThreatFramework.Core.CoreEntities;
using ThreatModeler.TF.Core.Global;

namespace ThreatFramework.Drift.Contract.CoreEntityDriftService
{
    public interface IYamlReaderRouter
    {
        // Multi-file readers
        Task<IEnumerable<Threat>> ReadThreatsAsync(IEnumerable<string> filePaths);
        Task<IEnumerable<Component>> ReadComponentsAsync(IEnumerable<string> filePaths);
        Task<IEnumerable<SecurityRequirement>> ReadSecurityRequirementsAsync(IEnumerable<string> filePaths);
        Task<IEnumerable<TestCase>> ReadTestCasesAsync(IEnumerable<string> filePaths);
        Task<IEnumerable<Property>> ReadPropertiesAsync(IEnumerable<string> filePaths);
        Task<IEnumerable<PropertyOption>> ReadPropertyOptionsAsync(IEnumerable<string> filePaths);
        Task<IEnumerable<Library>> ReadLibrariesAsync(IEnumerable<string> filePaths);

        // Single-file readers (new for all entities)
        Task<Threat> ReadThreatAsync(string filePath);
        Task<Component> ReadComponentAsync(string filePath);
        Task<SecurityRequirement> ReadSecurityRequirementAsync(string filePath);
        Task<TestCase> ReadTestCaseAsync(string filePath);
        Task<Property> ReadPropertyAsync(string filePath);
        Task<PropertyOption> ReadPropertyOptionAsync(string filePath);
        Task<Library> ReadLibraryAsync(string filePath);

        // Global types – already single-file
        Task<PropertyType> ReadPropertyTypeAsync(string filePath);
        Task<ComponentType> ReadComponentTypeAsync(string filePath);

        // Global types – multi-file
        Task<IEnumerable<PropertyType>> ReadPropertyTypesAsync(IEnumerable<string> filePaths);
        Task<IEnumerable<ComponentType>> ReadComponentTypeAsync(IEnumerable<string> filePaths);
    }
}
