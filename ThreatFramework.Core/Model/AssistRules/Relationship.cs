using System;
using System.Collections.Generic;
using ThreatFramework.Core;
using ThreatModeler.TF.Core.CustomException;
using ThreatModeler.TF.Core.Helper;

namespace ThreatModeler.TF.Core.Model.AssistRules
{
    public class Relationship : IFieldComparable<Relationship>
    {
        public string RelationshipName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Guid Guid { get; set; }
        public string ChineseRelationship { get; set; } = string.Empty;

        public List<FieldChange> CompareFields(Relationship other, IEnumerable<string> fields)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));

            var changes = new List<FieldChange>();

            foreach (var field in fields)
            {
                switch (field)
                {
                    // Value types (only if included in fields)
                    case nameof(Guid):
                        ComparisonHelper.Compare(changes, field, Guid, other.Guid);
                        break;

                    // Strings
                    case nameof(RelationshipName):
                    case nameof(Description):
                    case nameof(ChineseRelationship):
                        ComparisonHelper.CompareString(
                            changes,
                            field,
                            GetStringValue(field),
                            other.GetStringValue(field),
                            ignoreCase: false);
                        break;

                    default:
                        throw new FieldComparisonNotImplementedException(nameof(Relationship), field);
                }
            }

            return changes;
        }

        private string? GetStringValue(string fieldName) => fieldName switch
        {
            nameof(RelationshipName) => RelationshipName,
            nameof(Description) => Description,
            nameof(ChineseRelationship) => ChineseRelationship,
            _ => null
        };
    }
}
