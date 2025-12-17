using System;
using System.Collections.Generic;
using ThreatFramework.Core;
using ThreatModeler.TF.Core.CustomException;
using ThreatModeler.TF.Core.Helper;

namespace ThreatModeler.TF.Core.Model.AssistRules
{
    public class ResourceTypeValueRelationship : IFieldComparable<ResourceTypeValueRelationship>
    {
        public string SourceResourceTypeValue { get; set; } = string.Empty;
        public Guid RelationshipGuid { get; set; }
        public string TargetResourceTypeValue { get; set; } = string.Empty;
        public bool IsRequired { get; set; }
        public Guid LibraryId { get; set; }
        public bool IsDeleted { get; set; }

        public List<FieldChange> CompareFields(ResourceTypeValueRelationship other, IEnumerable<string> fields)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));

            var changes = new List<FieldChange>();

            foreach (var field in fields)
            {
                switch (field)
                {
                    // Value types
                    case nameof(RelationshipGuid):
                        ComparisonHelper.Compare(changes, field, RelationshipGuid, other.RelationshipGuid);
                        break;

                    case nameof(IsRequired):
                        ComparisonHelper.Compare(changes, field, IsRequired, other.IsRequired);
                        break;

                    case nameof(IsDeleted):
                        ComparisonHelper.Compare(changes, field, IsDeleted, other.IsDeleted);
                        break;

                    case nameof(LibraryId):
                        ComparisonHelper.Compare(changes, field, LibraryId, other.LibraryId);
                        break;

                    // Strings
                    case nameof(SourceResourceTypeValue):
                    case nameof(TargetResourceTypeValue):
                        ComparisonHelper.CompareString(
                            changes,
                            field,
                            GetStringValue(field),
                            other.GetStringValue(field),
                            ignoreCase: false);
                        break;

                    default:
                        throw new FieldComparisonNotImplementedException(nameof(ResourceTypeValueRelationship), field);
                }
            }

            return changes;
        }

        private string? GetStringValue(string fieldName) => fieldName switch
        {
            nameof(SourceResourceTypeValue) => SourceResourceTypeValue,
            nameof(TargetResourceTypeValue) => TargetResourceTypeValue,
            _ => null
        };
    }
}
