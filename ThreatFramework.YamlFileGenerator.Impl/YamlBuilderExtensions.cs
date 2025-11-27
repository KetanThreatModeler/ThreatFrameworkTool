namespace ThreatModeler.TF.YamlFileGenerator.Implementation
{
    public static class YamlBuilderExtensions
    {
        public static YamlBuilder AddQuoted(this YamlBuilder b, string key, string? value)
        {
            return b.AddChild($"{key}: \"{YamlFormatting.Escape(value ?? string.Empty)}\"");
        }

        public static YamlBuilder AddBool(this YamlBuilder b, string key, bool value)
        {
            return b.AddChild($"{key}: {YamlFormatting.ToYamlBool(value)}");
        }

        /// <summary>
        /// Adds labels from a comma-separated string as a YAML array.
        /// </summary>
        public static YamlBuilder AddLabels(this YamlBuilder b, string key, string? labelsCsv)
        {
            return b.AddChild($"{key}: {YamlFormatting.ToYamlArrayFromCsv(labelsCsv)}");
        }
    }
}
