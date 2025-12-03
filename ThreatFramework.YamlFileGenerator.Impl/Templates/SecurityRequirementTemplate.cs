using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Core.CoreEntities;
using ThreatModeler.TF.YamlFileGenerator.Implementation;

namespace ThreatFramework.YamlFileGenerator.Impl.Templates
{
    public static class SecurityRequirementTemplate
    {
        public static string Generate(SecurityRequirement securityRequirement)
        {
            var yaml = new YamlBuilder()
                .AddChild("kind: security-requirement")
                .AddQuoted("guid", securityRequirement.Guid.ToString())
                .AddQuoted("name", securityRequirement.Name)
                .AddQuoted("libraryGuid", securityRequirement.LibraryId.ToString())
                .AddLabels("labels", securityRequirement.Labels)
                .AddQuoted("RiskId", securityRequirement.RiskId.ToString())
                .AddQuoted("description", securityRequirement.Description ?? string.Empty)
                .AddQuoted("ChineseName", securityRequirement.ChineseName ?? string.Empty)
                .AddQuoted("ChineseDescription", securityRequirement.ChineseDescription ?? string.Empty)
                .AddParent("flags:", b2 =>
                {
                    b2.AddBool("isCompensatingControl", securityRequirement.IsCompensatingControl);
                    b2.AddBool("isHidden", securityRequirement.IsHidden);
                    b2.AddBool("isOverridden", securityRequirement.IsOverridden);
                })
                .Build();

            return yaml;
        }
    }
}
