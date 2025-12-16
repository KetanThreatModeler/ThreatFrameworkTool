using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatModeler.TF.Core.Model.AssistRules;

namespace ThreatModeler.TF.YamlFileGenerator.Implementation.Templates.AssistRule
{
    public static class ResourceTypeValuesTemplate
    {
        public static string Generate(ResourceTypeValues resourceTypeValues)
        {
            var yaml = new YamlBuilder()
                .AddChild("kind: assist-rule.resource-type-value")
                .AddQuoted("resourceName", resourceTypeValues.ResourceName)
                .AddQuoted("resourceTypeValue", resourceTypeValues.ResourceTypeValue)
                .AddQuoted("componentGuid", resourceTypeValues.ComponentGuid.ToString())
                .AddQuoted("libraryGuid", resourceTypeValues.LibraryId.ToString())
                .Build();

            return yaml;
        }
    }
}
