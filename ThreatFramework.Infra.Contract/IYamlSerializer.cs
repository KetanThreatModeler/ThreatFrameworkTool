using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatFramework.Infra.Contract
{
    public interface IYamlSerializer
    {
        T Deserialize<T>(string yaml);
    }
}
