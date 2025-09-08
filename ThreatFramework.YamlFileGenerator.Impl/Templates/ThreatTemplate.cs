using System;
using System.Collections.Generic;
using System.Linq;
using ThreatFramework.Core.CoreEntities;

namespace ThreatFramework.YamlFileGenerator.Impl.Templates
{
        internal static class ThreatTemplate
        {
            public static string Generate(Threat threat)
            {
                var labels = ParseLabels(threat.Labels);
                var labelsArray = labels.Any() ? $"[{string.Join(", ", labels)}]" : "[]";

                var yaml = new YamlBuilder()
                    .AddChild("kind: threat")
                    .AddChild("apiVersion: v1")
                    .AddParent("metadata:", b =>
                    {
                        b.AddChild($"guid: \"{threat.Guid}\"");
                        b.AddChild($"name: \"{EscapeYamlValue(threat.Name)}\"");
                        b.AddChild($"libraryGuid: \"{threat.LibraryGuid}\"");
                        b.AddChild($"labels: {labelsArray}");
                    })
                    .AddParent("spec:", b =>
                    {
                        b.AddChild($"description: \"{EscapeYamlValue(threat.Description ?? "")}\"");
                        b.AddChild($"reference: \"{EscapeYamlValue(threat.Reference ?? "")}\"");
                        b.AddChild($"intelligence: \"{EscapeYamlValue(threat.Intelligence ?? "")}\"");
                        b.AddParent("flags:", fb =>
                        {
                            fb.AddChild($"automated: {threat.Automated.ToString().ToLower()}");
                            fb.AddChild($"isHidden: {threat.IsHidden.ToString().ToLower()}");
                            fb.AddChild($"isOverridden: {threat.IsOverridden.ToString().ToLower()}");
                        });
                        b.AddParent("i18n:", ib =>
                        {
                            ib.AddParent("zh:", zb =>
                            {
                                zb.AddChild($"name: \"{EscapeYamlValue(threat.ChineseName ?? "")}\"");
                                zb.AddChild($"description: \"{EscapeYamlValue(threat.ChineseDescription ?? "")}\"");
                            });
                        });
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
