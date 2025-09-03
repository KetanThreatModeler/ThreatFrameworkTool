using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Core.Models;

namespace ThreatFramework.YamlFileGenerator.Impl.Templates
{
    public static class PropertyOptionYamlTemplate
    {
        public static string GeneratePropertyOptionYaml(PropertyOption propertyOption)
        {
            var yaml = new YamlBuilder()
                .AddChild("kind: property-option")
                .AddChild("apiVersion: v1")
                .AddParent("metadata:", b =>
                {
                    b.AddChild($"id: {propertyOption.Id}");
                    b.AddChild($"propertyGuid: \"{propertyOption.PropertyId}\"");
                })
                .AddParent("spec:", b =>
                {
                    b.AddChild($"optionText: \"{EscapeYamlValue(propertyOption.OptionText)}\"");
                    b.AddParent("flags:", b2 =>
                    {
                        b2.AddChild($"isDefault: {propertyOption.IsDefault.ToString().ToLower()}");
                        b2.AddChild($"isHidden: {propertyOption.IsHidden.ToString().ToLower()}");
                        b2.AddChild($"isOverridden: {propertyOption.IsOverridden.ToString().ToLower()}");
                    });

                    if (!string.IsNullOrEmpty(propertyOption.ChineseOptionText))
                    {
                        b.AddParent("i18n:", b2 =>
                        {
                            b2.AddParent("zh:", b3 =>
                            {
                                b3.AddChild($"optionText: \"{EscapeYamlValue(propertyOption.ChineseOptionText)}\"");
                            });
                        });
                    }
                })
                .Build();

            return yaml;
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
