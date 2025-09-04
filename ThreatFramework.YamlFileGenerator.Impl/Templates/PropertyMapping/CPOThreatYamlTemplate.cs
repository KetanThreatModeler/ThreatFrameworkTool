using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Core.Models.PropertyMapping;

namespace ThreatFramework.YamlFileGenerator.Impl.Templates.Mapping
{
    public static class CPOThreatYamlTemplate
    {
        public static string GenerateCPOThreatYaml(ComponentPropertyOptionThreatMapping cpoThreat)
        {
            var yaml = new YamlBuilder()
                .AddChild("kind: relation.cpo-threat")
                .AddChild("apiVersion: v1")
                .AddParent("metadata:", b =>
                {
                    b.AddChild($"id: {cpoThreat.Id}");
                })
                .AddParent("spec:", b =>
                {
                    b.AddChild($"componentPropertyOptionId: {cpoThreat.ComponentPropertyOptionId}");
                    b.AddChild($"threatGuid: \"{cpoThreat.ThreatId}\"");
                    b.AddParent("flags:", b2 =>
                    {
                        b2.AddChild($"isHidden: {cpoThreat.IsHidden.ToString().ToLower()}");
                        b2.AddChild($"isOverridden: {cpoThreat.IsOverridden.ToString().ToLower()}");
                    });
                })
                .Build();

            return yaml;
        }
    }
}
