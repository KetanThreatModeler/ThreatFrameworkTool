using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatModeler.TF.Core.Model.Global;

namespace ThreatModeler.TF.Infra.Contract.YamlRepository.Global
{
    public interface IYamlRiskReader
    {
        Task<IEnumerable<Risk>> GetRisksFromFilesAsync(IEnumerable<string> yamlFilePaths);
        Task<Risk> GetRiskFromFileAsync(string yamlFilePath);
    }
}
