using System;
using System.Collections.Generic;
using ThreatFramework.Core;
using ThreatModeler.TF.Core.CustomException;
using ThreatModeler.TF.Core.Helper;

namespace ThreatModeler.TF.Core.Model.AssistRules
{
    public class ResourceTypeValues : IFieldComparable<ResourceTypeValues>
    {
        public string ResourceName { get; set; } = string.Empty;
        public string ResourceTypeValue { get; set; } = string.Empty;
        public Guid ComponentGuid { get; set; }
        public Guid LibraryId { get; set; }

        public List<FieldChange> CompareFields(ResourceTypeValues other, IEnumerable<string> fields)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));

            var changes = new List<FieldChange>();

            foreach (var field in fields)
            {
                switch (field)
                {
                    // Value types
                    case nameof(ComponentGuid):
                        ComparisonHelper.Compare(changes, field, ComponentGuid, other.ComponentGuid);
                        break;

                    case nameof(LibraryId):
                        ComparisonHelper.Compare(changes, field, LibraryId, other.LibraryId);
                        break;

                    // Strings
                    case nameof(ResourceName):
                    case nameof(ResourceTypeValue):
                        ComparisonHelper.CompareString(
                            changes,
                            field,
                            GetStringValue(field),
                            other.GetStringValue(field),
                            ignoreCase: false);
                        break;

                    default:
                        throw new FieldComparisonNotImplementedException(nameof(ResourceTypeValues), field);
                }
            }

            return changes;
        }

        private string? GetStringValue(string fieldName) => fieldName switch
        {
            nameof(ResourceName) => ResourceName,
            nameof(ResourceTypeValue) => ResourceTypeValue,
            _ => null
        };
    }
}
