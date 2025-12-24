using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace ThreatModeler.TF.Infra.Implmentation.AssistRuleIndex.Builder
{
    public sealed class AssistRulesIndexYamlDocument
    {
        [YamlMember(Alias = "kind", ApplyNamingConventions = false)]
        public string Kind { get; set; } = "index.assist-rules";

        [YamlMember(Alias = "items", ApplyNamingConventions = false)]
        public List<AssistRuleIndexYamlItem> Items { get; set; } = new();
    }

    public sealed class AssistRuleIndexYamlItem
    {
        [YamlMember(Alias = "type", ApplyNamingConventions = false)]
        public string Type { get; set; }

        [YamlMember(Alias = "id", ApplyNamingConventions = false)]
        public int Id { get; set; }

        [YamlMember(Alias = "identity", ApplyNamingConventions = false)]
        public string Identity { get; set; }

        [YamlMember(Alias = "libraryGuid", ApplyNamingConventions = false)]
        public Guid LibraryGuid { get; set; }
    }
}