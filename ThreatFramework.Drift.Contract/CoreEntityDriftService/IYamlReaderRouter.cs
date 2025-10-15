using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Core.CoreEntities;

namespace ThreatFramework.Drift.Contract.CoreEntityDriftService
{
    public interface IYamlReaderRouter
    {
        Task<IEnumerable<Threat>> ReadThreatsAsync(IEnumerable<string> filePaths);
        Task<IEnumerable<Component>> ReadComponentsAsync(IEnumerable<string> filePaths);
        Task<IEnumerable<SecurityRequirement>> ReadSecurityRequirementsAsync(IEnumerable<string> filePaths);
        Task<IEnumerable<TestCase>> ReadTestCasesAsync(IEnumerable<string> filePaths);
        Task<IEnumerable<Property>> ReadPropertiesAsync(IEnumerable<string> filePaths);
        Task<IEnumerable<PropertyOption>> ReadPropertyOptionsAsync(IEnumerable<string> filePaths);
        Task<IEnumerable<Library>> ReadLibrariesAsync(IEnumerable<string> filePaths);
    }
}
