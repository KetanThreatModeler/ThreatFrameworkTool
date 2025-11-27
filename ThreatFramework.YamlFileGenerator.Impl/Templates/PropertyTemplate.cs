using ThreatFramework.Core.CoreEntities;

namespace ThreatModeler.TF.YamlFileGenerator.Implementation.Templates
{
    public static class PropertyTemplate
    {
        public static string Generate(Property property)
        {
            string yaml = new YamlBuilder()
                .AddChild("kind: property")
                .AddChild("apiVersion: v1")
                .AddQuoted("guid", property.Guid.ToString())
                .AddQuoted("name", property.Name)
                .AddQuoted("libraryGuid", property.LibraryGuid.ToString())
                .AddLabels("labels", property.Labels)
                .AddQuoted("propertyTypeGuid", property.PropertyTypeGuid.ToString())
                .AddQuoted("description", property.Description ?? string.Empty)
                .AddQuoted("ChineseName", property.ChineseName ?? string.Empty)
                .AddQuoted("ChineseDescription", property.ChineseDescription ?? string.Empty)
                .AddParent("flags:", b2 =>
                {
                    _ = b2.AddBool("isSelected", property.IsSelected);
                    _ = b2.AddBool("isOptional", property.IsOptional);
                    _ = b2.AddBool("isGlobal", property.IsGlobal);
                    _ = b2.AddBool("isHidden", property.IsHidden);
                    _ = b2.AddBool("isOverridden", property.IsOverridden);
                })
                .Build();

            return yaml;
        }
    }
}
