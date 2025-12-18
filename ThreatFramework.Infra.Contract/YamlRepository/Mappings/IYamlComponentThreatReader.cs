using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatModeler.TF.Core.Model.ComponentMapping;

namespace ThreatModeler.TF.Infra.Contract.YamlRepository.Mappings
{
    public interface IYamlComponentThreatReader
    {
        Task<List<ComponentThreatMapping>> GetAllAsync(string folderPath = null, CancellationToken ct = default);
        Task<ComponentThreatMapping> GetFromFileAsync(string yamlFilePath);
    }
}
