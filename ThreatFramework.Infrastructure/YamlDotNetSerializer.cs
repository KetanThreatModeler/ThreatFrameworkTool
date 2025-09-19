using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Infra.Contract;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ThreatFramework.Infrastructure
{
    public class YamlDotNetSerializer : IYamlSerializer
    {
        private readonly IDeserializer _deserializer;
        public YamlDotNetSerializer()
        {
            _deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties() // robust against extra fields
                .Build();
        }

        public T Deserialize<T>(string yaml) => _deserializer.Deserialize<T>(yaml);
    }
}
