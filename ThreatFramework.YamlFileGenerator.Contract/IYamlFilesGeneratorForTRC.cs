using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatFramework.YamlFileGenerator.Contract
{
    public interface IYamlFilesGeneratorForTRC
    {
        Task GenerateForLibraryIdsAsync(string outputFolderPath, IEnumerable<Guid> libraryIds, bool pushToRemote = false);
        Task GenerateForReadOnlyLibraryAsync(string outputFolderPath, bool pushToRemote = false);
    }
}
