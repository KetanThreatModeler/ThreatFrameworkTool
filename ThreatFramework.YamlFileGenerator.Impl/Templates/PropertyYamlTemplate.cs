using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Core.Models.CoreEntities;

namespace ThreatFramework.YamlFileGenerator.Impl.Templates
{
    public static class PropertyYamlTemplate
    {
        public static string GeneratePropertyYaml(Property property)
        {
            var labels = ParseLabels(property.Labels);
            var labelsArray = labels.Any() ? $"[{string.Join(", ", labels)}]" : "[]";

            var yaml = new YamlBuilder()
                .AddChild("kind: property")
                .AddChild("apiVersion: v1")
                .AddParent("metadata:", b =>
                {
                    b.AddChild($"guid: \"{property.Guid}\"");
                    b.AddChild($"name: \"{EscapeYamlValue(property.Name)}\"");
                    b.AddChild($"libraryGuid: \"{property.LibraryId}\"");
                    b.AddChild($"labels: {labelsArray}");
                })
                .AddParent("spec:", b =>
                {
                    b.AddChild($"propertyTypeGuid: \"{property.PropertyTypeId}\"");
                    b.AddParent("flags:", b2 =>
                    {
                        b2.AddChild($"isSelected: {property.IsSelected.ToString().ToLower()}");
                        b2.AddChild($"isOptional: {property.IsOptional.ToString().ToLower()}");
                        b2.AddChild($"isGlobal: {property.IsGlobal.ToString().ToLower()}");
                        b2.AddChild($"isHidden: {property.IsHidden.ToString().ToLower()}");
                        b2.AddChild($"isOverridden: {property.IsOverridden.ToString().ToLower()}");
                    });
                    b.AddChild($"description: \"{EscapeYamlValue(property.Description ?? "")}\"");

                    if (!string.IsNullOrEmpty(property.ChineseName) || !string.IsNullOrEmpty(property.ChineseDescription))
                    {
                        b.AddParent("i18n:", b2 =>
                        {
                            b2.AddParent("zh:", b3 =>
                            {
                                b3.AddChild($"name: \"{EscapeYamlValue(property.ChineseName ?? "")}\"");
                                b3.AddChild($"description: \"{EscapeYamlValue(property.ChineseDescription ?? "")}\"");
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
