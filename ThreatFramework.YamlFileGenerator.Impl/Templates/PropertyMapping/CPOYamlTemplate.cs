using ThreatFramework.Core.Models.PropertyMapping;

namespace ThreatFramework.YamlFileGenerator.Impl.Templates.Mapping
{
    public static class CPOYamlTemplate
    {
        public static string GenerateComponentPropertyOptionYaml(ComponentPropertyOptionMapping componentPropertyOption)
        {
            var yaml = new YamlBuilder()
                .AddChild("kind: relation.component-property-option")
                .AddParent("metadata:", b =>
                {
                    b.AddChild($"id: {componentPropertyOption.Id}");
                })
                .AddParent("spec:", b =>
                {
                    b.AddChild($"componentPropertyId: {componentPropertyOption.ComponentPropertyId}");
                    b.AddChild($"propertyOptionId: {componentPropertyOption.PropertyOptionId}");
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
