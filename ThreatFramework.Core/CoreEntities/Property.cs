using System;
using System.Collections.Generic;
using ThreatModeler.TF.Core.CustomException;
using ThreatModeler.TF.Core.Helper; // Using your specific helper namespace

namespace ThreatFramework.Core.CoreEntities
{
    public class Property : IFieldComparable<Property>
    {
        public int Id { get; set; }
        public Guid Guid { get; set; }
        public Guid LibraryGuid { get; set; }
        public Guid PropertyTypeGuid { get; set; }

        // Excluded from comparison (likely a joined/display value)
        public string PropertyTypeName { get; set; }

        // --- Booleans ---
        public bool IsSelected { get; set; }
        public bool IsOptional { get; set; }
        public bool IsGlobal { get; set; }
        public bool IsHidden { get; set; }
        public bool IsOverridden { get; set; }

        // Excluded from comparison
        public DateTime CreatedDate { get; set; }
        public DateTime? LastUpdated { get; set; }

        public string Name { get; set; }
        public string? ChineseName { get; set; }
        public string? Labels { get; set; }
        public string? Description { get; set; }
        public string? ChineseDescription { get; set; }

        public List<FieldChange> CompareFields(Property other, IEnumerable<string> fields)
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
                    case nameof(PropertyTypeGuid):
                    case nameof(IsSelected):
                    case nameof(IsOptional):
                    case nameof(IsGlobal):
                    case nameof(IsHidden):
                    case nameof(IsOverridden):
                        CompareValueTypes(changes, field, other);
                        break;

                    // --- GROUP 2: Case-Insensitive Strings ---
                    case nameof(Name):
                        ComparisonHelper.CompareString(changes, field, this.Name, other.Name, ignoreCase: true);
                        break;

                    // --- GROUP 3: Standard Strings (Case-Sensitive) ---
                    case nameof(ChineseName):
                    case nameof(Labels):
                    case nameof(Description):
                    case nameof(ChineseDescription):
                        string? s1 = GetStringValue(field);
                        string? s2 = other.GetStringValue(field);
                        ComparisonHelper.CompareString(changes, field, s1, s2, ignoreCase: false);
                        break;

                    // --- ERROR HANDLING ---
                    default:
                        throw new FieldComparisonNotImplementedException(nameof(Property), field);
                }
            }

            return changes;
        }

        // --- Private Helper: Value Types ---
        private void CompareValueTypes(List<FieldChange> changes, string field, Property other)
        {
            switch (field)
            {
                // Guids
                case nameof(Guid): ComparisonHelper.Compare(changes, field, this.Guid, other.Guid); break;
                case nameof(LibraryGuid): ComparisonHelper.Compare(changes, field, this.LibraryGuid, other.LibraryGuid); break;
                case nameof(PropertyTypeGuid): ComparisonHelper.Compare(changes, field, this.PropertyTypeGuid, other.PropertyTypeGuid); break;

                // Booleans
                case nameof(IsSelected): ComparisonHelper.Compare(changes, field, this.IsSelected, other.IsSelected); break;
                case nameof(IsOptional): ComparisonHelper.Compare(changes, field, this.IsOptional, other.IsOptional); break;
                case nameof(IsGlobal): ComparisonHelper.Compare(changes, field, this.IsGlobal, other.IsGlobal); break;
                case nameof(IsHidden): ComparisonHelper.Compare(changes, field, this.IsHidden, other.IsHidden); break;
                case nameof(IsOverridden): ComparisonHelper.Compare(changes, field, this.IsOverridden, other.IsOverridden); break;
            }
        }

        // --- Private Helper: Strings ---
        private string? GetStringValue(string fieldName) => fieldName switch
        {
            nameof(ChineseName) => ChineseName,
            nameof(Labels) => Labels,
            nameof(Description) => Description,
            nameof(ChineseDescription) => ChineseDescription,
            _ => null
        };
    }
}