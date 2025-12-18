using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatModeler.TF.Core.Model.AssistRules;

namespace ThreatModeler.TF.YamlFileGenerator.Implementation.Templates.AssistRule
{
    public static class RelationshipTemplate
    {
        public static string Generate(Relationship relationship)
        {
            var yaml = new YamlBuilder()
                .AddChild("kind: assist-rule.relationship")
                .AddQuoted("relationshipGuid", relationship.Guid.ToString())
                .AddQuoted("relationshipName", relationship.RelationshipName)
                .AddQuoted("description", relationship.Description)
                .AddQuoted("chineseRelationship", relationship.ChineseRelationship)
                .Build();

            return yaml;
        }
    }
}
