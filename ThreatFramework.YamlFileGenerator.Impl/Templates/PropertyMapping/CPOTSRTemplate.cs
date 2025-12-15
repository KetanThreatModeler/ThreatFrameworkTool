using ThreatModeler.TF.Core.Model.PropertyMapping;
using ThreatModeler.TF.YamlFileGenerator.Implementation;

namespace ThreatFramework.YamlFileGenerator.Impl.Templates.PropertyMapping
{
    public static class CPOTSRTemplate
    {
        public static string Generate(ComponentPropertyOptionThreatSecurityRequirementMapping cpoSecurityRequirement)
        {
            var yaml = new YamlBuilder()
                .AddChild("kind: relation.cpo-threat-sr")
                .AddQuoted("componentGuid", cpoSecurityRequirement.ComponentGuid.ToString())
                .AddQuoted("propertyGuid", cpoSecurityRequirement.PropertyGuid.ToString())
                .AddQuoted("propertyOptionGuid", cpoSecurityRequirement.PropertyOptionGuid.ToString())
                .AddQuoted("threatGuid", cpoSecurityRequirement.ThreatGuid.ToString())
                .AddQuoted("securityRequirementGuid", cpoSecurityRequirement.SecurityRequirementGuid.ToString())
                .AddParent("flags:", b2 =>
                {
                    b2.AddBool("isHidden", cpoSecurityRequirement.IsHidden);
                    b2.AddBool("isOverridden", cpoSecurityRequirement.IsOverridden);
                })
                .Build();

            return yaml;
        }
    }
}