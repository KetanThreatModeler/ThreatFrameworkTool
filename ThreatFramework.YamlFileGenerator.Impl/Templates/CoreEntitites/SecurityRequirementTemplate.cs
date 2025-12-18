using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatModeler.TF.Core.Helper;
using ThreatModeler.TF.Core.Model.CoreEntities;
using ThreatModeler.TF.YamlFileGenerator.Implementation;

namespace ThreatModeler.TF.YamlFileGenerator.Implementation.Templates.CoreEntitites
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
                .AddLabels("labels", securityRequirement.Labels.ToDelimitedString())
                .AddQuoted("riskName", securityRequirement.RiskName.ToString())
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
