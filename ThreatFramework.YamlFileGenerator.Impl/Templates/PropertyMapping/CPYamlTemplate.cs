using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Core.Models.PropertyMapping;

namespace ThreatFramework.YamlFileGenerator.Impl.Templates.Mapping
{
    public static class CPYamlTemplate
    {
        public static string GenerateComponentPropertyYaml(ComponentPropertyMapping componentProperty)
        {
            var yaml = new YamlBuilder()
                .AddChild("kind: relation.component-property")
                .AddParent("metadata:", b =>
                {
                    b.AddChild($"id: {componentProperty.Id}");
                })
                .AddParent("spec:", b =>
                {
                    b.AddChild($"componentGuid: \"{componentProperty.ComponentId}\"");
                    b.AddChild($"propertyGuid: \"{componentProperty.PropertyId}\"");
                    b.AddParent("flags:", b2 =>
                    {
                        b2.AddChild($"isOptional: {componentProperty.IsOptional.ToString().ToLower()}");
                        b2.AddChild($"isHidden: {componentProperty.IsHidden.ToString().ToLower()}");
                        b2.AddChild($"isOverridden: {componentProperty.IsOverridden.ToString().ToLower()}");
                    });
                })
                .Build();

            return yaml;
        }
    }
}
