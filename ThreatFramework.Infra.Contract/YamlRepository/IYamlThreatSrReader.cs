using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Core.ComponentMapping;

namespace ThreatFramework.Infra.Contract.YamlRepository
{
    public interface IYamlThreatSrReader
    {
        Task<List<ThreatSecurityRequirementMapping>> GetAllAsync(string folderPath = null, CancellationToken ct = default);
        Task<ThreatSecurityRequirementMapping> GetFromFileAsync(string yamlFilePath);
    }
}
