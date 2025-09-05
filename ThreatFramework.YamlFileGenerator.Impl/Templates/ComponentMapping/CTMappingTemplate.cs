using ThreatFramework.Core.Models.ComponentMapping;

namespace ThreatFramework.YamlFileGenerator.Impl.Templates.ComponentMapping
{
    public static class CTMappingTemplate
    {
        public static string Generate(ComponentThreatMapping ctMapping)
        {
            var yaml = new YamlBuilder()
                .AddChild("kind: relation.component-threat")
                .AddChild("apiVersion: v1")
                .AddParent("metadata:", b =>
                {
                    b.AddChild($"id: {ctMapping.Id}");
                })
                .AddParent("spec:", b =>
                {
                    b.AddChild($"componentGuid: \"{ctMapping.ComponentGuid}\"");
                    b.AddChild($"threatGuid: \"{ctMapping.ThreatGuid}\"");
                    b.AddParent("flags:", b2 =>
                    {
                        b2.AddChild($"isHidden: {ctMapping.IsHidden.ToString().ToLower()}");
                        b2.AddChild($"isOverridden: {ctMapping.IsOverridden.ToString().ToLower()}");
                        b2.AddChild($"usedForMitigation: {ctMapping.UsedForMitigation.ToString().ToLower()}");
                    });
                })
                .Build();

            return yaml;
        }
    }
}
