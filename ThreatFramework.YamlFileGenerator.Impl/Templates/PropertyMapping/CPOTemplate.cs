using ThreatModeler.TF.Core.Model.PropertyMapping;
using ThreatModeler.TF.YamlFileGenerator.Implementation;

namespace ThreatFramework.YamlFileGenerator.Impl.Templates.PropertyMapping
{
    public static class CPOTemplate
    {
        public static string Generate(ComponentPropertyOptionMapping componentPropertyOption)
        {
            var yaml = new YamlBuilder()
                .AddChild("kind: relation.component-property-option")
                .AddQuoted("componentGuid", componentPropertyOption.ComponentGuid.ToString())
                .AddQuoted("propertyGuid", componentPropertyOption.PropertyGuid.ToString())
                .AddQuoted("propertyOptionGuid", componentPropertyOption.PropertyOptionGuid.ToString())
                .AddParent("flags:", b2 =>
                {
                    b2.AddBool("isDefault", componentPropertyOption.IsDefault);
                    b2.AddBool("isHidden", componentPropertyOption.IsHidden);
                    b2.AddBool("isOverridden", componentPropertyOption.IsOverridden);
                })
                .Build();

            return yaml;
        }
    }
}
