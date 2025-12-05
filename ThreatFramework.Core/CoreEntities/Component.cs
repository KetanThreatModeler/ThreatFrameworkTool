using System;
using System.Collections.Generic;
using ThreatModeler.TF.Core.CustomException;
using ThreatModeler.TF.Core.Helper; // Using your helper namespace

namespace ThreatFramework.Core.CoreEntities
{
    public class Component : IFieldComparable<Component>
    {
        public Guid Guid { get; set; }
        public Guid LibraryGuid { get; set; }
        public Guid ComponentTypeGuid { get; set; }

        // --- Booleans ---
        public bool IsHidden { get; set; }
        public bool IsOverridden { get; set; }

        // --- Strings ---
        public string Name { get; set; } = string.Empty;
        public string ImagePath { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ChineseDescription { get; set; } = string.Empty;

        // --- Lists (Updated from String to List<string>) ---
        public List<string> Labels { get; set; } = new List<string>();

        public List<FieldChange> CompareFields(Component other, IEnumerable<string> fields)
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
                    case nameof(ComponentTypeGuid):
                    case nameof(IsHidden):
                    case nameof(IsOverridden):
                        CompareValueTypes(changes, field, other);
                        break;

                    // --- GROUP 2: Case-Insensitive Strings ---
                    case nameof(Name):
                        ComparisonHelper.CompareString(changes, field, this.Name, other.Name, ignoreCase: true);
                        break;

                    // --- GROUP 3: Standard Strings (Case-Sensitive) ---
                    case nameof(ImagePath):
                    case nameof(Version):
                    case nameof(Description):
                    case nameof(ChineseDescription):
                        string? s1 = GetStringValue(field);
                        string? s2 = other.GetStringValue(field);
                        ComparisonHelper.CompareString(changes, field, s1, s2, ignoreCase: false);
                        break;

                    // --- GROUP 4: Lists ---
                    case nameof(Labels):
                        ComparisonHelper.CompareList(changes, field, this.Labels, other.Labels);
                        break;

                    // --- ERROR HANDLING ---
                    default:
                        throw new FieldComparisonNotImplementedException(nameof(Component), field);
                }
            }

            return changes;
        }

        // --- Private Helper: Value Types ---
        private void CompareValueTypes(List<FieldChange> changes, string field, Component other)
        {
            switch (field)
            {
                case nameof(Guid): ComparisonHelper.Compare(changes, field, this.Guid, other.Guid); break;
                case nameof(LibraryGuid): ComparisonHelper.Compare(changes, field, this.LibraryGuid, other.LibraryGuid); break;
                case nameof(ComponentTypeGuid): ComparisonHelper.Compare(changes, field, this.ComponentTypeGuid, other.ComponentTypeGuid); break;
                case nameof(IsHidden): ComparisonHelper.Compare(changes, field, this.IsHidden, other.IsHidden); break;
                case nameof(IsOverridden): ComparisonHelper.Compare(changes, field, this.IsOverridden, other.IsOverridden); break;
            }
        }

        // --- Private Helper: Strings ---
        private string? GetStringValue(string fieldName) => fieldName switch
        {
            nameof(ImagePath) => ImagePath,
            nameof(Version) => Version,
            nameof(Description) => Description,
            nameof(ChineseDescription) => ChineseDescription,
            _ => null
        };
    }
}