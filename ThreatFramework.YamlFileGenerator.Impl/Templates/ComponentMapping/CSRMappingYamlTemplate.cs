using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Core.Models.ComponentMapping;

namespace ThreatFramework.YamlFileGenerator.Impl.Templates.ComponentMapping
{
    public static class CSRMappingYamlTemplate
    {
        public static string GenerateComponentSecurityRequirementYaml(ComponentSecurityRequirementMapping csrMapping)
        {
            var yaml = new YamlBuilder()
                .AddChild("kind: relation.component-sr")
                .AddChild("apiVersion: v1")
                .AddParent("spec:", b =>
                {
                    b.AddChild($"componentGuid: \"{csrMapping.ComponentId}\"");
                    b.AddChild($"securityRequirementGuid: \"{csrMapping.SecurityRequirementId}\"");
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
