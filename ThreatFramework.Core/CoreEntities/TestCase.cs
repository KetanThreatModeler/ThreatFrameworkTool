using System;
using System.Collections.Generic;
using ThreatModeler.TF.Core.CustomException;
using ThreatModeler.TF.Core.Helper; // Using your specific helper namespace

namespace ThreatFramework.Core.CoreEntities
{
    public class TestCase : IFieldComparable<TestCase>
    {
        public int Id { get; set; }
        public Guid Guid { get; set; }
        public Guid LibraryId { get; set; } // Note: Guid type, named LibraryId

        // --- Booleans ---
        public bool IsHidden { get; set; }
        public bool IsOverridden { get; set; }

        public string Name { get; set; }
        public string? ChineseName { get; set; }
        public string? Labels { get; set; }
        public string? Description { get; set; }
        public string? ChineseDescription { get; set; }

        public List<FieldChange> CompareFields(TestCase other, IEnumerable<string> fields)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));

            var changes = new List<FieldChange>();

            foreach (var field in fields)
            {
                switch (field)
                {
                    // --- GROUP 1: Value Types (Guid, Bool) ---
                    case nameof(Guid):
                    case nameof(LibraryId):
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
                        throw new FieldComparisonNotImplementedException(nameof(TestCase), field);
                }
            }

            return changes;
        }

        // --- Private Helper: Value Types ---
        private void CompareValueTypes(List<FieldChange> changes, string field, TestCase other)
        {
            switch (field)
            {
                // Guids
                case nameof(Guid): ComparisonHelper.Compare(changes, field, this.Guid, other.Guid); break;
                case nameof(LibraryId): ComparisonHelper.Compare(changes, field, this.LibraryId, other.LibraryId); break;

                // Booleans
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