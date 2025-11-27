using ThreatFramework.Core.ComponentMapping;
using ThreatModeler.TF.YamlFileGenerator.Implementation;

namespace ThreatFramework.YamlFileGenerator.Impl.Templates.ComponentMapping
{
    public static class CTHMappingTemplate
    {
        public static string Generate(ComponentThreatMapping ctMapping)
        {
            var yaml = new YamlBuilder()
                .AddChild("kind: relation.component-threat")
                .AddQuoted("componentGuid", ctMapping.ComponentGuid.ToString())
                .AddQuoted("threatGuid", ctMapping.ThreatGuid.ToString())
                .AddParent("flags:", b2 =>
                {
                    b2.AddBool("isHidden", ctMapping.IsHidden);
                    b2.AddBool("isOverridden", ctMapping.IsOverridden);
                    b2.AddBool("usedForMitigation", ctMapping.UsedForMitigation);
                })
                .Build();

            return yaml;
        }
    }
}