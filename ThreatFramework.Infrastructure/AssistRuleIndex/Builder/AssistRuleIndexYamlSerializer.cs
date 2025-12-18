using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatModeler.TF.Infra.Contract.AssistRuleIndex.Builder;
using ThreatModeler.TF.Infra.Contract.AssistRuleIndex.Model;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ThreatModeler.TF.Infra.Implmentation.AssistRuleIndex.Builder
{
    public sealed class AssistRuleIndexYamlSerializer : IAssistRuleIndexSerializer
    {
        private readonly ISerializer _serializer;
        private readonly IDeserializer _deserializer;

        public AssistRuleIndexYamlSerializer()
        {
            _serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults)
                .Build();

            _deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();
        }

        public string Serialize(IReadOnlyList<AssistRuleIndexEntry> entries)
        {
            entries ??= Array.Empty<AssistRuleIndexEntry>();

            var doc = new AssistRulesIndexYamlDocument
            {
                Kind = "index.assist-rules",
                Items = entries.Select(e => new AssistRuleIndexYamlItem
                {
                    Type = e.Type.ToString(),
                    Id = e.Id,
                    Identity = e.Identity,
                    LibraryGuid = e.LibraryGuid
                }).ToList()
            };

            return _serializer.Serialize(doc);
        }

        public IReadOnlyList<AssistRuleIndexEntry> Deserialize(string yaml)
        {
            if (string.IsNullOrWhiteSpace(yaml))
                return Array.Empty<AssistRuleIndexEntry>();

            var doc = _deserializer.Deserialize<AssistRulesIndexYamlDocument>(yaml);

            if (doc?.Items == null || doc.Items.Count == 0)
                return Array.Empty<AssistRuleIndexEntry>();

            if (!string.Equals(doc.Kind, "index.assist-rules", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Invalid kind '{doc.Kind}'. Expected 'index.assist-rules'.");

            var result = new List<AssistRuleIndexEntry>(doc.Items.Count);

            foreach (var i in doc.Items)
            {
                if (i == null) continue;
                if (string.IsNullOrWhiteSpace(i.Type) ||
                    string.IsNullOrWhiteSpace(i.Id) ||
                    string.IsNullOrWhiteSpace(i.Identity))
                    continue;

                result.Add(new AssistRuleIndexEntry
                {
                    Type = ParseType(i.Type),
                    Id = i.Id,
                    Identity = i.Identity,
                    LibraryGuid = i.LibraryGuid
                });
            }

            return result;
        }

        private static AssistRuleType ParseType(string type)
        {
            if (Enum.TryParse(type, ignoreCase: true, out AssistRuleType parsed))
                return parsed;

            throw new InvalidOperationException($"Unknown AssistRuleType '{type}'.");
        }
    }
}
