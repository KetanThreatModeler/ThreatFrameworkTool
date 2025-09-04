using ThreatFramework.Core.Models.PropertyMapping;

namespace ThreatFramework.YamlFileGenerator.Impl.Templates.Mapping
{
    public static class CPOTSecurityRequirementYamlTemplate
    {
        public static string GenerateCPOSecurityRequirementYaml(ComponentPropertyOptionThreatSecurityRequirementMapping cpoSecurityRequirement)
        {
            var yaml = new YamlBuilder()
                .AddChild("kind: relation.cpo-threat-sr")
                .AddChild("apiVersion: v1")
                .AddParent("metadata:", b =>
                {
                    b.AddChild($"id: {cpoSecurityRequirement.Id}");
                })
                .AddParent("spec:", b =>
                {
                    b.AddChild($"cpoThreatId: {cpoSecurityRequirement.ComponentPropertyOptionThreatId}");
                    b.AddChild($"securityRequirementGuid: \"{cpoSecurityRequirement.SecurityRequirementId}\"");
                    b.AddParent("flags:", b2 =>
                    {
                        b2.AddChild($"isHidden: {cpoSecurityRequirement.IsHidden.ToString().ToLower()}");
                        b2.AddChild($"isOverridden: {cpoSecurityRequirement.IsOverridden.ToString().ToLower()}");
                    });
                })
                .Build();

            return yaml;
        }
    }
}
