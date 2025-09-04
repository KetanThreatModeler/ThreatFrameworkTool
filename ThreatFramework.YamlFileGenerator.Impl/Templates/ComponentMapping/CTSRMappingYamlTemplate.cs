using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Core.Models.ComponentMapping;

namespace ThreatFramework.YamlFileGenerator.Impl.Templates.ComponentMapping
{
    public static class CTSRMappingYamlTemplate
    {
        public static string GenerateComponentThreatSecurityRequirementYaml(ComponentThreatSecurityRequirementMapping ctsrMapping)
        {
            var yaml = new YamlBuilder()
                .AddChild("kind: relation.component-threat-sr")
                .AddChild("apiVersion: v1")
                .AddParent("metadata:", b =>
                {
                    b.AddChild($"id: {ctsrMapping.Id}");
                })
                .AddParent("spec:", b =>
                {
                    b.AddChild($"componentThreatId: {ctsrMapping.ComponentThreatId}");
                    b.AddChild($"securityRequirementGuid: \"{ctsrMapping.SecurityRequirementId}\"");
                    b.AddParent("flags:", b2 =>
                    {
                        b2.AddChild($"isHidden: {ctsrMapping.IsHidden.ToString().ToLower()}");
                        b2.AddChild($"isOverridden: {ctsrMapping.IsOverridden.ToString().ToLower()}");
                    });
                })
                .Build();

            return yaml;
        }
    }
}
