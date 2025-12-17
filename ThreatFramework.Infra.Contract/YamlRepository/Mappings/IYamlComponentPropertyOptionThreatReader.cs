using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatModeler.TF.Core.Model.PropertyMapping;

namespace ThreatModeler.TF.Infra.Contract.YamlRepository.Mappings
{
    public  interface IYamlComponentPropertyOptionThreatReader
    {
        Task<List<ComponentPropertyOptionThreatMapping>> GetAllAsync(string folderPath = null, CancellationToken ct = default);
        Task<ComponentPropertyOptionThreatMapping> GetFromFileAsync(string yamlFilePath);
    }
}
