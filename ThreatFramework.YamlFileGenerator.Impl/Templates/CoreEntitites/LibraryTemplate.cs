using ThreatModeler.TF.Core.Model.CoreEntities;
using ThreatModeler.TF.YamlFileGenerator.Implementation;

namespace ThreatModeler.TF.YamlFileGenerator.Implementation.Templates.CoreEntitites
{
    public static class LibraryTemplate
    {
        public static string Generate(Library library)
        {
            var yaml = new YamlBuilder()
                .AddChild("kind: library")
                .AddChild("apiVersion: v1")
                .AddQuoted("guid", library.Guid.ToString())
                .AddQuoted("name", library.Name)
                .AddQuoted("version", library.Version ?? string.Empty)
                .AddLabels("labels", library.Labels)
                .AddQuoted("description", library.Description ?? string.Empty)
                .AddBool("readonly", library.Readonly)
                .AddBool("isDefault", library.IsDefault)
                .AddQuoted("imageUrl", library.ImageURL ?? string.Empty)
                .Build();

            return yaml;
        }
    }
}
