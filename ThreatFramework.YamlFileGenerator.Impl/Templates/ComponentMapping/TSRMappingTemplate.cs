using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatModeler.TF.Core.Model.ThreatMapping;
using ThreatModeler.TF.YamlFileGenerator.Implementation;

namespace ThreatFramework.YamlFileGenerator.Impl.Templates.ComponentMapping
{
    public static class TSRMappingTemplate
    {
        public static string Generate(ThreatSecurityRequirementMapping tsrMapping)
        {
            var yaml = new YamlBuilder()
                .AddChild("kind: relation.threat-sr")
                .AddQuoted("threatGuid", tsrMapping.ThreatGuid.ToString())
                .AddQuoted("securityRequirementGuid", tsrMapping.SecurityRequirementGuid.ToString())
                .AddParent("flags:", b2 =>
                {
                    b2.AddBool("isHidden", tsrMapping.IsHidden);
                    b2.AddBool("isOverridden", tsrMapping.IsOverridden);
                })
                .Build();

            return yaml;
        }
    }
}
