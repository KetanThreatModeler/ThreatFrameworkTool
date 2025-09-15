using ThreatFramework.Core.PropertyMapping;

namespace ThreatFramework.YamlFileGenerator.Impl.Templates.PropertyMapping
{
    public static class CPOTemplate
    {
        public static string Generate(ComponentPropertyOptionMapping componentPropertyOption)
        {
            var yaml = new YamlBuilder()
                .AddChild("kind: relation.component-property-option")
                .AddParent("spec:", b =>
                {
                    b.AddChild($"componentGuid: {componentPropertyOption.ComponentGuid}");
                    b.AddChild($"propertyGuid: {componentPropertyOption.PropertyGuid}");
                    b.AddChild($"propertyOptionGuid: {componentPropertyOption.PropertyOptionGuid}");
                    b.AddParent("flags:", b2 =>
                    {
                        b2.AddChild($"isDefault: {componentPropertyOption.IsDefault.ToString().ToLower()}");
                        b2.AddChild($"isHidden: {componentPropertyOption.IsHidden.ToString().ToLower()}");
                        b2.AddChild($"isOverridden: {componentPropertyOption.IsOverridden.ToString().ToLower()}");
                    });
                })
                .Build();

            return yaml;
        }
    }
}
