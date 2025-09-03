using ThreatFramework.Core.Models;

namespace ThreatFramework.YamlFileGenerator.Impl.Templates
{
    public static class LibraryYamlTemplate
    {
        public static string GenerateLibraryYaml(Library library)
        {
            var labels = ParseLabels(library.Labels);
            var labelsArray = labels.Any() ? $"[{string.Join(", ", labels)}]" : "[]";

            var yaml = new YamlBuilder()
                .AddChild("kind: library")
                .AddChild("apiVersion: v1")
                .AddParent("metadata:", b =>
                {
                    b.AddChild($"guid: \"{library.Guid}\"");
                    b.AddChild($"name: \"{EscapeYamlValue(library.Name)}\"");
                    b.AddChild($"version: \"{EscapeYamlValue(library.Version ?? "")}\"");
                    b.AddChild($"labels: {labelsArray}");
                })
                .AddParent("spec:", b =>
                {
                    b.AddChild($"description: \"{EscapeYamlValue(library.Description ?? "")}\"");
                    b.AddChild($"readonly: {library.Readonly.ToString().ToLower()}");
                    b.AddChild($"isDefault: {library.IsDefault.ToString().ToLower()}");
                    b.AddChild($"imageUrl: \"{EscapeYamlValue(library.ImageURL ?? "")}\"");
                    b.AddChild($"createdAt: \"{library.DateCreated:yyyy-MM-ddTHH:mm:ssZ}\"");
                    b.AddChild($"updatedAt: \"{library.LastUpdated:yyyy-MM-ddTHH:mm:ssZ}\"");
                })
                .Build();

            return yaml;
        }

        private static IEnumerable<string> ParseLabels(string? labels)
        {
            if (string.IsNullOrEmpty(labels))
                return Enumerable.Empty<string>();

            return labels.Split(',', StringSplitOptions.RemoveEmptyEntries)
                         .Select(l => $"\"{EscapeYamlValue(l.Trim())}\"");
        }

        private static string EscapeYamlValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value ?? "";

            return value.Replace("\"", "\\\"")
                        .Replace("\n", "\\n")
                        .Replace("\r", "\\r")
                        .Replace("\t", "\\t");
        }
    }
}
