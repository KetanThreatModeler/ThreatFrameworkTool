using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Core.CoreEntities;

namespace ThreatFramework.Infra.Contract.YamlRepository.CoreEntity
{
    public interface IYamlComponentReader
    {
        Task<IEnumerable<Component>> GetComponentsFromFilesAsync(IEnumerable<string> yamlFilePaths);
    }
}
