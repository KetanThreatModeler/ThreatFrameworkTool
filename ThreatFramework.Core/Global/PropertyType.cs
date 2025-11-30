using System;
using System.Collections.Generic;
using ThreatFramework.Core;
using ThreatModeler.TF.Core.CustomException;
using ThreatModeler.TF.Core.Helper; // Using your specific helper namespace

namespace ThreatModeler.TF.Core.Global
{
    public class PropertyType : IFieldComparable<PropertyType>
    {
        public Guid Guid { get; set; }
        public string Name { get; set; } = string.Empty;

        public List<FieldChange> CompareFields(PropertyType other, IEnumerable<string> fields)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));

            var changes = new List<FieldChange>();

            foreach (var field in fields)
            {
                switch (field)
                {
                    // --- Value Types ---
                    case nameof(Guid):
                        ComparisonHelper.Compare(changes, field, this.Guid, other.Guid);
                        break;

                    // --- Case-Insensitive Strings ---
                    case nameof(Name):
                        ComparisonHelper.CompareString(changes, field, this.Name, other.Name, ignoreCase: true);
                        break;

                    // --- ERROR HANDLING ---
                    default:
                        throw new FieldComparisonNotImplementedException(nameof(PropertyType), field);
                }
            }

            return changes;
        }
    }
}