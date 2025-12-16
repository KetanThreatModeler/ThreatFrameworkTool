using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatModeler.TF.Core.Model.AssistRules;

namespace ThreatModeler.TF.YamlFileGenerator.Implementation.Templates.AssistRule
{
    public static class ResourceTypeValueRelationshipTemplate
    {
        public static string Generate(ResourceTypeValueRelationship relationship)
        {
            var yaml = new YamlBuilder()
                .AddChild("kind: assist-rule.resource-type-value-relationship")
                .AddQuoted("sourceResourceTypeValue", relationship.SourceResourceTypeValue)
                .AddQuoted("targetResourceTypeValue", relationship.TargetResourceTypeValue)
                .AddQuoted("relationshipGuid", relationship.RelationshipGuid.ToString())
                .AddQuoted("libraryGuid", relationship.LibraryId.ToString())
                .AddParent("flags:", b2 =>
                {
                    b2.AddBool("isRequired", relationship.IsRequired);
                    b2.AddBool("isDeleted", relationship.IsDeleted);
                })
                .Build();

            return yaml;
        }
    }
}
