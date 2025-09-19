using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Core.CoreEntities;

namespace ThreatFramework.YamlFileGenerator.Impl.Templates
{
    public static class ComponentTemplate
    {
        public static string Generate(Component component)
        {
            var labels = ParseLabels(component.Labels);
            var labelsArray = labels.Any() ? $"[{string.Join(", ", labels)}]" : "[]";

            var yaml = new YamlBuilder()
                .AddChild("kind: component")
                .AddParent("metadata:", b =>
                {
                    b.AddChild($"guid: \"{component.Guid}\"");
                    b.AddChild($"name: \"{EscapeYamlValue(component.Name)}\"");
                    b.AddChild($"libraryId: {component.LibraryGuid}");
                    b.AddChild($"labels: {labelsArray}");
                    b.AddChild($"version: \"{EscapeYamlValue(component.Version ?? "")}\"");
                })
                .AddParent("spec:", b =>
                {
                    b.AddChild($"description: \"{EscapeYamlValue(component.Description ?? "")}\"");
                    b.AddChild($"imagePath: \"{EscapeYamlValue(component.ImagePath ?? "")}\"");
                    b.AddParent("flags:", b2 =>
                    {
                        b2.AddChild($"isHidden: {component.IsHidden.ToString().ToLower()}");
                        b2.AddChild($"isOverridden: {component.IsOverridden.ToString().ToLower()}");
                    });

                    if (!string.IsNullOrEmpty(component.ChineseDescription))
                    {
                        b.AddParent("i18n:", b2 =>
                        {
                            b2.AddParent("zh:", b3 =>
                            {
                                b3.AddChild($"name: \"{EscapeYamlValue(component.ChineseDescription)}\"");
                                b3.AddChild($"description: \"{EscapeYamlValue(component.ChineseDescription)}\"");
                            });
                        });
                    }
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

