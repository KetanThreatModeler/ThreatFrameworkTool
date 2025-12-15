using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatModeler.TF.Core.Model.Global;

namespace ThreatModeler.TF.YamlFileGenerator.Implementation.Templates.Global
{
    public static class PropertyOptionTemplate
    {
        public static string Generate(PropertyOption propertyOption)
        {
            var yaml = new YamlBuilder()
                .AddChild("kind: property-option")
                .AddQuoted("id", propertyOption.Guid.ToString())
                .AddQuoted("propertyGuid", propertyOption.PropertyGuid.ToString())
                .AddQuoted("optionText", propertyOption.OptionText ?? string.Empty)
                .AddQuoted("chineseOptionText", propertyOption.ChineseOptionText ?? string.Empty)
                .AddParent("flags:", b2 =>
                {
                    b2.AddBool("isDefault", propertyOption.IsDefault);
                    b2.AddBool("isHidden", propertyOption.IsHidden);
                    b2.AddBool("isOverridden", propertyOption.IsOverridden);
                })
                .Build();

            return yaml;
        }
    }
}