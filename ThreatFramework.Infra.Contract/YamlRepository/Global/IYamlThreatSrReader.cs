using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatModeler.TF.Core.Model.ThreatMapping;

namespace ThreatModeler.TF.Infra.Contract.YamlRepository.Global
{
    public interface IYamlThreatSrReader
    {
        Task<List<ThreatSecurityRequirementMapping>> GetAllAsync(string folderPath = null, CancellationToken ct = default);
        Task<ThreatSecurityRequirementMapping> GetFromFileAsync(string yamlFilePath);
    }
}
