using System;
using System.Collections.Generic;
using ThreatFramework.Core; // Assuming IFieldComparable is here
using ThreatModeler.TF.Core.CustomException;
using ThreatModeler.TF.Core.Helper; // Using your specific helper namespace

namespace ThreatModeler.TF.Core.Global
{
    public class ComponentType : IFieldComparable<ComponentType>
    {
        public Guid Guid { get; set; }
        public Guid LibraryGuid { get; set; }

        // --- Booleans ---
        public bool IsHidden { get; set; }
        public bool IsSecurityControl { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ChineseName { get; set; } = string.Empty;
        public string ChineseDescription { get; set; } = string.Empty;

        public List<FieldChange> CompareFields(ComponentType other, IEnumerable<string> fields)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));

            var changes = new List<FieldChange>();

            foreach (var field in fields)
            {
                switch (field)
                {
                    // --- GROUP 1: Value Types (Guids & Bools) ---
                    case nameof(Guid):
                    case nameof(LibraryGuid):
                    case nameof(IsHidden):
                    case nameof(IsSecurityControl):
                        CompareValueTypes(changes, field, other);
                        break;

                    // --- GROUP 2: Case-Insensitive Strings ---
                    case nameof(Name):
                        ComparisonHelper.CompareString(changes, field, this.Name, other.Name, ignoreCase: true);
                        break;

                    // --- GROUP 3: Standard Strings (Case-Sensitive) ---
                    case nameof(Description):
                    case nameof(ChineseName):
                    case nameof(ChineseDescription):
                        string? s1 = GetStringValue(field);
                        string? s2 = other.GetStringValue(field);
                        ComparisonHelper.CompareString(changes, field, s1, s2, ignoreCase: false);
                        break;

                    // --- ERROR HANDLING ---
                    default:
                        throw new FieldComparisonNotImplementedException(nameof(ComponentType), field);
                }
            }

            return changes;
        }

        // --- Private Helper: Value Types ---
        private void CompareValueTypes(List<FieldChange> changes, string field, ComponentType other)
        {
            switch (field)
            {
                // Guids
                case nameof(Guid): ComparisonHelper.Compare(changes, field, this.Guid, other.Guid); break;
                case nameof(LibraryGuid): ComparisonHelper.Compare(changes, field, this.LibraryGuid, other.LibraryGuid); break;

                // Booleans
                case nameof(IsHidden): ComparisonHelper.Compare(changes, field, this.IsHidden, other.IsHidden); break;
                case nameof(IsSecurityControl): ComparisonHelper.Compare(changes, field, this.IsSecurityControl, other.IsSecurityControl); break;
            }
        }

        // --- Private Helper: Strings ---
        private string? GetStringValue(string fieldName) => fieldName switch
        {
            nameof(Description) => Description,
            nameof(ChineseName) => ChineseName,
            nameof(ChineseDescription) => ChineseDescription,
            _ => null
        };
    }
}