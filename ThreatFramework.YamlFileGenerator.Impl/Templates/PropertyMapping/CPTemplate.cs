using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatModeler.TF.Core.Model.PropertyMapping;
using ThreatModeler.TF.YamlFileGenerator.Implementation;

namespace ThreatFramework.YamlFileGenerator.Impl.Templates.PropertyMapping
{
    public static class CPTemplate
    {
        public static string Generate(ComponentPropertyMapping componentProperty)
        {
            var yaml = new YamlBuilder()
                .AddChild("kind: relation.component-property")
                .AddQuoted("componentGuid", componentProperty.ComponentGuid.ToString())
                .AddQuoted("propertyGuid", componentProperty.PropertyGuid.ToString())
                .AddParent("flags:", b2 =>
                {
                    b2.AddBool("isOptional", componentProperty.IsOptional);
                    b2.AddBool("isHidden", componentProperty.IsHidden);
                    b2.AddBool("isOverridden", componentProperty.IsOverridden);
                })
                .Build();

            return yaml;
        }
    }
}