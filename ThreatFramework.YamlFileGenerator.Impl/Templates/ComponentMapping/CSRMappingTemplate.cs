using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Core.ComponentMapping;

namespace ThreatFramework.YamlFileGenerator.Impl.Templates.ComponentMapping
{
    public static class CSRMappingTemplate
    {
        public static string Generate(ComponentSecurityRequirementMapping csrMapping)
        {
            var yaml = new YamlBuilder()
                .AddChild("kind: relation.component-sr")
                .AddChild("apiVersion: v1")
                .AddParent("spec:", b =>
                {
                    b.AddChild($"componentGuid: \"{csrMapping.ComponentGuid}\"");
                    b.AddChild($"securityRequirementGuid: \"{csrMapping.SecurityRequirementGuid}\"");
                    b.AddParent("flags:", b2 =>
                    {
                        b2.AddChild($"isHidden: {csrMapping.IsHidden.ToString().ToLower()}");
                        b2.AddChild($"isOverridden: {csrMapping.IsOverridden.ToString().ToLower()}");
                    });
                })
                .Build();

            return yaml;
        }
    }
}
