using System;
using System.Collections.Generic;
using ThreatFramework.Core;
using ThreatModeler.TF.Core.CustomException;
using ThreatModeler.TF.Core.Helper;

namespace ThreatModeler.TF.Core.Model.CoreEntities
{
    public class Threat : IFieldComparable<Threat>
    {
        // --- Identifiers ---
        public string RiskName { get; set; } = string.Empty;
        public Guid Guid { get; set; }
        public Guid LibraryGuid { get; set; }

        // --- Booleans ---
        public bool Automated { get; set; }
        public bool IsHidden { get; set; }
        public bool IsOverridden { get; set; }

        // --- Strings ---
        public string Name { get; set; } = string.Empty;
        public string ChineseName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public string Intelligence { get; set; } = string.Empty;
        public string ChineseDescription { get; set; } = string.Empty;

        // --- Lists ---
        public List<string> Labels { get; set; } = new();

        public List<FieldChange> CompareFields(Threat other, IEnumerable<string> fields)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));

            var changes = new List<FieldChange>();

            foreach (var field in fields)
            {
                switch (field)
                {
                    // Identifiers / value types
                    case nameof(Guid):
                    case nameof(LibraryGuid):
                    case nameof(Automated):
                    case nameof(IsHidden):
                    case nameof(IsOverridden):
                        CompareValueTypes(changes, field, other);
                        break;

                    // RiskName is a string identifier
                    case nameof(RiskName):
                        ComparisonHelper.CompareString(
                            changes,
                            field,
                            RiskName,
                            other.RiskName,
                            ignoreCase: true);
                        break;

                    // Case-insensitive string comparison for Name
                    case nameof(Name):
                        ComparisonHelper.CompareString(
                            changes,
                            field,
                            Name,
                            other.Name,
                            ignoreCase: true);
                        break;

                    // Other strings (case-sensitive)
                    case nameof(Description):
                    case nameof(ChineseName):
                    case nameof(Reference):
                    case nameof(Intelligence):
                    case nameof(ChineseDescription):
                        var s1 = GetStringValue(field);
                        var s2 = other.GetStringValue(field);
                        ComparisonHelper.CompareString(
                            changes,
                            field,
                            s1,
                            s2,
                            ignoreCase: false);
                        break;

                    // Lists
                    case nameof(Labels):
                        ComparisonHelper.CompareList(changes, field, Labels, other.Labels);
                        break;

                    default:
                        throw new FieldComparisonNotImplementedException(nameof(Threat), field);
                }
            }

            return changes;
        }

        private void CompareValueTypes(List<FieldChange> changes, string field, Threat other)
        {
            switch (field)
            {
                case nameof(Guid):
                    ComparisonHelper.Compare(changes, field, Guid, other.Guid);
                    break;

                case nameof(LibraryGuid):
                    ComparisonHelper.Compare(changes, field, LibraryGuid, other.LibraryGuid);
                    break;

                case nameof(Automated):
                    ComparisonHelper.Compare(changes, field, Automated, other.Automated);
                    break;

                case nameof(IsHidden):
                    ComparisonHelper.Compare(changes, field, IsHidden, other.IsHidden);
                    break;

                case nameof(IsOverridden):
                    ComparisonHelper.Compare(changes, field, IsOverridden, other.IsOverridden);
                    break;
            }
        }

        private string? GetStringValue(string fieldName) => fieldName switch
        {
            nameof(Description) => Description,
            nameof(ChineseName) => ChineseName,
            nameof(Reference) => Reference,
            nameof(Intelligence) => Intelligence,
            nameof(ChineseDescription) => ChineseDescription,
            _ => null
        };
    }
}
