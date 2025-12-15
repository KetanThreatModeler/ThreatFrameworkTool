using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatModeler.TF.Core.Model.PropertyMapping;

namespace ThreatFramework.Infra.Contract.YamlRepository
{
    public interface IYamlCpoThreatSrReader
    {
        Task<List<ComponentPropertyOptionThreatSecurityRequirementMapping>> GetAllAsync(string folderPath = null, CancellationToken ct = default);
        Task<ComponentPropertyOptionThreatSecurityRequirementMapping> GetFromFileAsync(string yamlFilePath);
    }
}
