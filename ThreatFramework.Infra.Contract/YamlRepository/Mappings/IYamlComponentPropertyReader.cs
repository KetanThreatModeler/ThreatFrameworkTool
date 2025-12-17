using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatModeler.TF.Core.Model.PropertyMapping;

namespace ThreatModeler.TF.Infra.Contract.YamlRepository.Mappings
{
    public interface IYamlComponentPropertyReader
    {
        Task<List<ComponentPropertyMapping>> GetAllAsync(
           string folderPath = null, CancellationToken ct = default);
        Task<ComponentPropertyMapping> GetFromFileAsync(string yamlFilePath);
    }
}
