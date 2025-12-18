using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatModeler.TF.Core.Model.ComponentMapping;
using ThreatModeler.TF.YamlFileGenerator.Implementation;

namespace ThreatFramework.YamlFileGenerator.Impl.Templates.ComponentMapping
{
    public static class CTSRMappingTemplate
    {
        public static string Generate(ComponentThreatSecurityRequirementMapping ctsrMapping)
        {
            var yaml = new YamlBuilder()
                .AddChild("kind: relation.component-threat-sr")
                .AddQuoted("componentGuid", ctsrMapping.ComponentGuid.ToString())
                .AddQuoted("threatGuid", ctsrMapping.ThreatGuid.ToString())
                .AddQuoted("securityRequirementGuid", ctsrMapping.SecurityRequirementGuid.ToString())
                .AddParent("flags:", b2 =>
                {
                    b2.AddBool("isHidden", ctsrMapping.IsHidden);
                    b2.AddBool("isOverridden", ctsrMapping.IsOverridden);
                })
                .Build();

            return yaml;
        }
    }
}
