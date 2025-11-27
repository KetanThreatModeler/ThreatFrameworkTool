using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatModeler.TF.YamlFileGenerator.Implementation
{
    public static class YamlFormatting
    {
        public static string Escape(string? value)
        {
            if (string.IsNullOrEmpty(value)) return value ?? string.Empty;

            return value
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }

        /// <summary>
        /// Convert a comma-separated label string into a YAML array literal: ["a","b"] or [].
        /// </summary>
        public static string ToYamlArrayFromCsv(string? labelsCsv)
        {
            if (string.IsNullOrWhiteSpace(labelsCsv)) return "[]";

            var items = labelsCsv
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => $"\"{Escape(s.Trim())}\"");

            return $"[{string.Join(", ", items)}]";
        }

        public static string ToYamlBool(bool value) => value ? "true" : "false";
    }
}
