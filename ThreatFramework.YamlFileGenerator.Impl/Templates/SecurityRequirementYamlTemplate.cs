using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Core.Models;

namespace ThreatFramework.YamlFileGenerator.Impl.Templates
{
    public static class SecurityRequirementYamlTemplate
    {
        public static string GenerateSecurityRequirementYaml(SecurityRequirement securityRequirement)
        {
            var labels = ParseLabels(securityRequirement.Labels);
            var labelsArray = labels.Any() ? $"[{string.Join(", ", labels)}]" : "[]";

            var yaml = new YamlBuilder()
                .AddChild("kind: security-requirement")
                .AddParent("metadata:", b =>
                {
                    b.AddChild($"guid: \"{securityRequirement.Guid}\"");
                    b.AddChild($"name: \"{EscapeYamlValue(securityRequirement.Name)}\"");
                    b.AddChild($"libraryGuid: \"{securityRequirement.LibraryId}\"");
                    b.AddChild($"labels: {labelsArray}");
                })
                .AddParent("spec:", b =>
                {
                    b.AddChild($"description: \"{EscapeYamlValue(securityRequirement.Description ?? "")}\"");
                    b.AddParent("flags:", b2 =>
                    {
                        b2.AddChild($"isCompensatingControl: {securityRequirement.IsCompensatingControl.ToString().ToLower()}");
                        b2.AddChild($"isHidden: {securityRequirement.IsHidden.ToString().ToLower()}");
                        b2.AddChild($"isOverridden: {securityRequirement.IsOverridden.ToString().ToLower()}");
                    });

                    if (!string.IsNullOrEmpty(securityRequirement.ChineseName) || !string.IsNullOrEmpty(securityRequirement.ChineseDescription))
                    {
                        b.AddParent("i18n:", b2 =>
                        {
                            b2.AddParent("zh:", b3 =>
                            {
                                b3.AddChild($"name: \"{EscapeYamlValue(securityRequirement.ChineseName ?? "")}\"");
                                b3.AddChild($"description: \"{EscapeYamlValue(securityRequirement.ChineseDescription ?? "")}\"");
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
