using ThreatModeler.TF.Core.Helper;
using ThreatModeler.TF.Core.Model.CoreEntities;
using ThreatModeler.TF.YamlFileGenerator.Implementation;

namespace ThreatModeler.TF.YamlFileGenerator.Implementation.Templates.CoreEntitites
{
    public static class ComponentTemplate
    {
        public static string Generate(Component component)
        {
            var yaml = new YamlBuilder()
                .AddChild("kind: component")
                .AddChild("apiVersion: v1")
                .AddQuoted("guid", component.Guid.ToString())
                .AddQuoted("name", component.Name)
                .AddQuoted("libraryGuid", component.LibraryGuid.ToString())
                .AddLabels("labels", component.Labels.ToDelimitedString())
                .AddQuoted("componentTypeGuid", component.ComponentTypeGuid.ToString())
                .AddQuoted("version", component.Version ?? string.Empty)
                .AddQuoted("description", component.Description ?? string.Empty)
                .AddQuoted("imagePath", component.ImagePath ?? string.Empty)
                .AddQuoted("ChineseDescription", component.ChineseDescription ?? string.Empty)
                .AddParent("flags:", b2 =>
                {
                    _ = b2.AddBool("isHidden", component.IsHidden);
                    _ = b2.AddBool("isOverridden", component.IsOverridden);
                })
                .Build();

            return yaml;
        }
    }
}

