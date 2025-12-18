using ThreatModeler.TF.Core.Model.PropertyMapping;
using ThreatModeler.TF.YamlFileGenerator.Implementation;

namespace ThreatFramework.YamlFileGenerator.Impl.Templates.PropertyMapping
{
    public static class CPOThreatTemplate
    {
        public static string Generate(ComponentPropertyOptionThreatMapping cpoThreat)
        {
            var yaml = new YamlBuilder()
                .AddChild("kind: relation.cpo-threat")
                .AddQuoted("componentGuid", cpoThreat.ComponentGuid.ToString())
                .AddQuoted("propertyGuid", cpoThreat.PropertyGuid.ToString())
                .AddQuoted("propertyOptionGuid", cpoThreat.PropertyOptionGuid.ToString())
                .AddQuoted("threatGuid", cpoThreat.ThreatGuid.ToString())
                .AddParent("flags:", b2 =>
                {
                    b2.AddBool("isHidden", cpoThreat.IsHidden);
                    b2.AddBool("isOverridden", cpoThreat.IsOverridden);
                })
                .Build();

            return yaml;
        }
    }
}