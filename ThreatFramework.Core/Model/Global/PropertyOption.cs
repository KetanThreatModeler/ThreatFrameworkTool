using System;
using System.Collections.Generic;
using ThreatFramework.Core; // Assuming IFieldComparable is here
using ThreatModeler.TF.Core.CustomException;
using ThreatModeler.TF.Core.Helper;

namespace ThreatModeler.TF.Core.Model.Global
{
    public class PropertyOption : IFieldComparable<PropertyOption>
    {
        public int PropertyGuid { get; set; }
        public Guid Guid { get; set; }

        // --- Booleans ---
        public bool IsDefault { get; set; }
        public bool IsHidden { get; set; }
        public bool IsOverridden { get; set; }
        public string OptionText { get; set; }
        public string ChineseOptionText { get; set; }

        public List<FieldChange> CompareFields(PropertyOption other, IEnumerable<string> fields)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));

            var changes = new List<FieldChange>();

            foreach (var field in fields)
            {
                switch (field)
                {
                    // --- GROUP 1: Value Types (Int?, Guid, Bool) ---
                    case nameof(PropertyGuid):
                    case nameof(Guid):
                    case nameof(IsDefault):
                    case nameof(IsHidden):
                    case nameof(IsOverridden):
                        CompareValueTypes(changes, field, other);
                        break;

                    // --- GROUP 2: Case-Insensitive Strings (Acting as Name) ---
                    case nameof(OptionText):
                        ComparisonHelper.CompareString(changes, field, OptionText, other.OptionText, ignoreCase: true);
                        break;

                    // --- GROUP 3: Standard Strings (Case-Sensitive) ---
                    case nameof(ChineseOptionText):
                        string? s1 = GetStringValue(field);
                        string? s2 = other.GetStringValue(field);
                        ComparisonHelper.CompareString(changes, field, s1, s2, ignoreCase: false);
                        break;

                    // --- ERROR HANDLING ---
                    default:
                        throw new FieldComparisonNotImplementedException(nameof(PropertyOption), field);
                }
            }

            return changes;
        }

        // --- Private Helper: Value Types ---
        private void CompareValueTypes(List<FieldChange> changes, string field, PropertyOption other)
        {
            switch (field)
            {
                // Identifiers
                case nameof(PropertyGuid): ComparisonHelper.Compare(changes, field, PropertyGuid, other.PropertyGuid); break;
                case nameof(Guid): ComparisonHelper.Compare(changes, field, Guid, other.Guid); break;

                // Booleans
                case nameof(IsDefault): ComparisonHelper.Compare(changes, field, IsDefault, other.IsDefault); break;
                case nameof(IsHidden): ComparisonHelper.Compare(changes, field, IsHidden, other.IsHidden); break;
                case nameof(IsOverridden): ComparisonHelper.Compare(changes, field, IsOverridden, other.IsOverridden); break;
            }
        }

        // --- Private Helper: Strings ---
        private string? GetStringValue(string fieldName) => fieldName switch
        {
            nameof(ChineseOptionText) => ChineseOptionText,
            _ => null
        };
    }
}