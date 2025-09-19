using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Core.CoreEntities;

namespace ThreatFramework.Drift.Contract
{
    public interface IYamlReaderRouter
    {
        Task<IEnumerable<Threat>> ReadThreatsAsync(IEnumerable<string> filePaths, CancellationToken ct);
        Task<IEnumerable<Component>> ReadComponentsAsync(IEnumerable<string> filePaths, CancellationToken ct);
        Task<IEnumerable<SecurityRequirement>> ReadSecurityRequirementsAsync(IEnumerable<string> filePaths, CancellationToken ct);
        Task<IEnumerable<TestCase>> ReadTestCasesAsync(IEnumerable<string> filePaths, CancellationToken ct);
        Task<IEnumerable<Property>> ReadPropertiesAsync(IEnumerable<string> filePaths, CancellationToken ct);
        Task<IEnumerable<PropertyOption>> ReadPropertyOptionsAsync(IEnumerable<string> filePaths, CancellationToken ct);
        Task<IEnumerable<Library>> ReadLibrariesAsync(IEnumerable<string> filePaths, CancellationToken ct);
    }
}
