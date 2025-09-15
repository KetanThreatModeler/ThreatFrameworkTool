using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Core.PropertyMapping;

namespace ThreatFramework.Infra.Contract.YamlRepository
{
    public interface IYamlComponentPropertyOptionReader
    {
        Task<IReadOnlyList<ComponentPropertyOptionMapping>> GetAllAsync(string folderPath, CancellationToken ct = default);
    }
}
