using ThreatFramework.Core.Models.PropertyMapping;

namespace ThreatFramework.YamlFileGenerator.Impl.Templates.PropertyMapping
{
    public static class CPOTSRTemplate
    {
        public static string GenerateCPOTSR(ComponentPropertyOptionThreatSecurityRequirementMapping cpoSecurityRequirement)
        {
            var yaml = new YamlBuilder()
                .AddChild("kind: relation.cpo-threat-sr")
                .AddChild("apiVersion: v1")
                .AddParent("spec:", b =>
                {
                    b.AddChild($"componentGuid: {cpoSecurityRequirement.ComponentGuid}");
                    b.AddChild($"propertyGuid: {cpoSecurityRequirement.PropertyGuid}");
                    b.AddChild($"propertyOptionGuid: {cpoSecurityRequirement.PropertyOptionGuid}");
                    b.AddChild($"threatGuid: {cpoSecurityRequirement.ThreatGuid}");
                    b.AddChild($"securityRequirementGuid: \"{cpoSecurityRequirement.SecurityRequirementGuid}\"");
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
