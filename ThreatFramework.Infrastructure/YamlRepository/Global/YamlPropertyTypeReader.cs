using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Infrastructure.YamlRepository;
using ThreatModeler.TF.Core.Global;
using ThreatModeler.TF.Infra.Contract.YamlRepository.Global;

namespace ThreatModeler.TF.Infra.Implmentation.YamlRepository.Global
{
    public class YamlPropertyTypeReader : YamlReaderBase, IYamlPropertyTypeReader
    {
        public Task<PropertyType> GetPropertyTypeFromFileAsync(string yamlFilePath)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<PropertyType>> GetPropertyTypesFromFilesAsync(IEnumerable<string> yamlFilePaths)
        {
            throw new NotImplementedException();
        }
    }
}
