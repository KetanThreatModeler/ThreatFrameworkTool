using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Core.Models.ComponentMapping;

namespace ThreatFramework.YamlFileGenerator.Impl.Templates.ComponentMapping
{
    public static class TSRMappingTemplate
    {
        public static string Generate(ThreatSecurityRequirementMapping tsrMapping)
        {
            var yaml = new YamlBuilder()
                .AddChild("kind: relation.threat-sr")
                .AddChild("apiVersion: v1")
                .AddParent("spec:", b =>
                {
                    b.AddChild($"threatGuid: \"{tsrMapping.ThreatGuid}\"");
                    b.AddChild($"securityRequirementGuid: \"{tsrMapping.SecurityRequirementGuid}\"");
                    b.AddParent("flags:", b2 =>
                    {
                        b2.AddChild($"isHidden: {tsrMapping.IsHidden.ToString().ToLower()}");
                        b2.AddChild($"isOverridden: {tsrMapping.IsOverridden.ToString().ToLower()}");
                    });
                })
                .Build();

            return yaml;
        }
    }
}
