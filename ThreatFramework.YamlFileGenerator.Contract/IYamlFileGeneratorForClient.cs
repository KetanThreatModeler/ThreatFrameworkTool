using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatFramework.YamlFileGenerator.Contract
{
    public interface IYamlFileGeneratorForClient
    {
        Task GenerateAsync(string outputFolderPath);
    }
}
