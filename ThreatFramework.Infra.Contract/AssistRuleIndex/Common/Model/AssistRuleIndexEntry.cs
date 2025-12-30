using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatModeler.TF.Infra.Contract.AssistRuleIndex.Common.Model
{
    public sealed class AssistRuleIndexEntry
    {
        public string Identity { get; init; }      // RelationshipGuid string OR ResourceTypeValue
        public Guid LibraryGuid { get; init; }     // Guid.Empty for Relationships
        public int Id { get; init; }                // prefix_int
        public AssistRuleType Type { get; init; }   // Relationship / ResourceTypeValues
    }
}
