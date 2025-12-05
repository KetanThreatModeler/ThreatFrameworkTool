using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Core.CoreEntities;
using ThreatModeler.TF.Core.Global;

namespace ThreatModeler.TF.Infra.Contract.YamlRepository.Global
{
    public interface IYamlRiskReader
    {
        Task<IEnumerable<Risk>> GetRisksFromFilesAsync(IEnumerable<string> yamlFilePaths);
        Task<Risk> GetRiskFromFileAsync(string yamlFilePath);
    }
}
