using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatModeler.TF.Core.Model.ComponentMapping;
using ThreatModeler.TF.YamlFileGenerator.Implementation;

namespace ThreatFramework.YamlFileGenerator.Impl.Templates.ComponentMapping
{
    public static class CSRMappingTemplate
    {
        public static string Generate(ComponentSecurityRequirementMapping csrMapping)
        {
            var yaml = new YamlBuilder()
                .AddChild("kind: relation.component-sr")
                .AddQuoted("componentGuid", csrMapping.ComponentGuid.ToString())
                .AddQuoted("securityRequirementGuid", csrMapping.SecurityRequirementGuid.ToString())
                .AddParent("flags:", b2 =>
                {
                    b2.AddBool("isHidden", csrMapping.IsHidden);
                    b2.AddBool("isOverridden", csrMapping.IsOverridden);
                })
                .Build();

            return yaml;
        }
    }
}
