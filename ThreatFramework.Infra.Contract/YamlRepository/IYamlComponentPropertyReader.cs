using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Core.PropertyMapping;

namespace ThreatFramework.Infra.Contract.YamlRepository
{
    public interface IYamlComponentPropertyReader
    {
        Task<List<ComponentPropertyMapping>> GetAllAsync(
           string folderPath = null, CancellationToken ct = default);
    }
}
