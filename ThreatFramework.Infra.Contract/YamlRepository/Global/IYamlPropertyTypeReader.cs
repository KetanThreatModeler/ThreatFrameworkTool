using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatModeler.TF.Core.Global;

namespace ThreatModeler.TF.Infra.Contract.YamlRepository.Global
{
    public interface IYamlPropertyTypeReader
    {
        Task<IEnumerable<PropertyType>> GetPropertyTypesFromFilesAsync(IEnumerable<string> yamlFilePaths);
        Task<PropertyType> GetPropertyTypeFromFileAsync(string yamlFilePath);
    }
}
