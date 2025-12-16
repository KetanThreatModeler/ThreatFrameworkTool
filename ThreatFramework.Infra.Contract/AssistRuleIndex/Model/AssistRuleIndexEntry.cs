using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatModeler.TF.Infra.Contract.AssistRuleIndex.Model
{
    public sealed class AssistRuleIndexEntry
    {
        public string Identity { get; init; }      // RelationshipGuid string OR ResourceTypeValue
        public Guid LibraryGuid { get; init; }     // Guid.Empty for Relationships
        public string Id { get; init; }            // prefix_int (e.g., REL_1 / RTV_10)
        public AssistRuleType Type { get; init; }  // Relationship / ResourceTypeValues
    }
}
