using ThreatModeler.TF.Core.Global;

namespace ThreatModeler.TF.YamlFileGenerator.Implementation.Templates.Global
{
    public static class PropertyTypeTemplate
    {
        public static string Generate(PropertyType propertyType)
        {
            if (propertyType == null)
                throw new ArgumentNullException(nameof(propertyType));

            var yaml = new YamlBuilder()
                .AddChild("kind: property-type")
                .AddQuoted("guid", propertyType.Guid.ToString())
                .AddQuoted("name", propertyType.Name ?? string.Empty)
                .Build();

            return yaml;
        }
    }
}
