using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Core.ComponentMapping;

namespace ThreatFramework.Infra.Contract.YamlRepository
{
    public interface IYamlComponentThreatSRReader
    {
        Task<List<ComponentThreatSecurityRequirementMapping>> GetAllAsync(string folderPath = null, CancellationToken ct = default);
        Task<ComponentThreatSecurityRequirementMapping> GetFromFileAsync(string yamlFilePath);
    }
}
