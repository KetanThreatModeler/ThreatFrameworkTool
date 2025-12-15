using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatModeler.TF.Core.Model.ComponentMapping;

namespace ThreatFramework.Infra.Contract.YamlRepository
{
    public interface IYamlComponentSRReader
    {
        Task<List<ComponentSecurityRequirementMapping>> GetAllComponentSRAsync(string folderPath = null, CancellationToken ct = default);
        Task<ComponentSecurityRequirementMapping> GetComponentSRFromFileAsync(string yamlFilePath);
    }
}
