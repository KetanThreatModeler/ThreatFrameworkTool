using ThreatFramework.Core.PropertyMapping;

namespace ThreatFramework.YamlFileGenerator.Impl.Templates.PropertyMapping
{
    public static class CPOThreatTemplate
    {
        public static string Generate(ComponentPropertyOptionThreatMapping cpoThreat)
        {
            var yaml = new YamlBuilder()
                .AddChild("kind: relation.cpo-threat")
                .AddChild("apiVersion: v1")
                .AddParent("spec:", b =>
                {
                    b.AddChild($"componentGuid: {cpoThreat.ComponentGuid}");
                    b.AddChild($"propertyGuid: {cpoThreat.PropertyGuid}");
                    b.AddChild($"propertyOptionGuid: {cpoThreat.PropertyOptionGuid}");
                    b.AddChild($"threatGuid: \"{cpoThreat.ThreatGuid}\"");
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
