using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Core.Models;

namespace ThreatFramework.YamlFileGenerator.Impl.Templates
{
    public static class TestCaseYamlTemplate
    {
        public static string GenerateTestCaseYaml(TestCase testCase)
        {
            var labels = ParseLabels(testCase.Labels);
            var labelsArray = labels.Any() ? $"[{string.Join(", ", labels)}]" : "[]";

            var yaml = new YamlBuilder()
                .AddChild("kind: test-case")
                .AddChild("apiVersion: v1")
                .AddParent("metadata:", b =>
                {
                    b.AddChild($"guid: \"{testCase.Guid}\"");
                    b.AddChild($"name: \"{EscapeYamlValue(testCase.Name)}\"");
                    b.AddChild($"libraryGuid: \"{testCase.LibraryId}\"");
                    b.AddChild($"labels: {labelsArray}");
                })
                .AddParent("spec:", b =>
                {
                    b.AddChild($"description: \"{EscapeYamlValue(testCase.Description ?? "")}\"");
                    b.AddParent("flags:", b2 =>
                    {
                        b2.AddChild($"isHidden: {testCase.IsHidden.ToString().ToLower()}");
                        b2.AddChild($"isOverridden: {testCase.IsOverridden.ToString().ToLower()}");
                    });

                    if (!string.IsNullOrEmpty(testCase.ChineseName) || !string.IsNullOrEmpty(testCase.ChineseDescription))
                    {
                        b.AddParent("i18n:", b2 =>
                        {
                            b2.AddParent("zh:", b3 =>
                            {
                                b3.AddChild($"name: \"{EscapeYamlValue(testCase.ChineseName ?? "")}\"");
                                b3.AddChild($"description: \"{EscapeYamlValue(testCase.ChineseDescription ?? "")}\"");
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
