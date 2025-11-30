using System;
using System.Collections.Generic;
using ThreatModeler.TF.Core.CustomException;
using ThreatModeler.TF.Core.Helper;

namespace ThreatFramework.Core.CoreEntities
{
    public class Threat : IFieldComparable<Threat>
    {
        // --- Identifiers ---
        public int Id { get; set; }
        public int RiskId { get; set; }
        public Guid Guid { get; set; }
        public Guid LibraryGuid { get; set; }

        // --- Booleans ---
        public bool Automated { get; set; }
        public bool IsHidden { get; set; }
        public bool IsOverridden { get; set; }

        // --- Dates ---
        public DateTime CreatedDate { get; set; }
        public DateTime? LastUpdated { get; set; }

        // --- Strings ---
        public string Name { get; set; } = string.Empty;
        public string ChineseName { get; set; } = string.Empty; 
        public string Description { get; set; } = string.Empty;
        public string Reference { get; set; }   = string.Empty;
        public string Intelligence { get; set; } = string.Empty;
        public string ChineseDescription { get; set; } = string.Empty;

        // --- Lists ---
        public List<string> Labels { get; set; } = new List<string>();

        public List<FieldChange> CompareFields(Threat other, IEnumerable<string> fields)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));

            var changes = new List<FieldChange>();

            foreach (var field in fields)
            {
                switch (field)
                {
                    case nameof(RiskId):
                    case nameof(Guid):
                    case nameof(LibraryGuid):
                    case nameof(Automated):
                    case nameof(IsHidden):
                    case nameof(IsOverridden):
                    case nameof(LastUpdated):
                        CompareValueTypes(changes, field, other);
                        break;

                    case nameof(Name):
                        ComparisonHelper.CompareString(changes, field, this.Name, other.Name, ignoreCase: true);
                        break;

                    case nameof(Description):
                    case nameof(ChineseName):
                    case nameof(Reference):
                    case nameof(Intelligence):
                    case nameof(ChineseDescription):
                        string? s1 = GetStringValue(field);
                        string? s2 = other.GetStringValue(field);
                        ComparisonHelper.CompareString(changes, field, s1, s2, ignoreCase: false);
                        break;

                    case nameof(Labels):
                        ComparisonHelper.CompareList(changes, field, this.Labels, other.Labels);
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
                case nameof(RiskId): ComparisonHelper.Compare(changes, field, this.RiskId, other.RiskId); break;
                case nameof(Guid): ComparisonHelper.Compare(changes, field, this.Guid, other.Guid); break;
                case nameof(LibraryGuid): ComparisonHelper.Compare(changes, field, this.LibraryGuid, other.LibraryGuid); break;
                case nameof(Automated): ComparisonHelper.Compare(changes, field, this.Automated, other.Automated); break;
                case nameof(IsHidden): ComparisonHelper.Compare(changes, field, this.IsHidden, other.IsHidden); break;
                case nameof(IsOverridden): ComparisonHelper.Compare(changes, field, this.IsOverridden, other.IsOverridden); break;
                case nameof(LastUpdated): ComparisonHelper.Compare(changes, field, this.LastUpdated, other.LastUpdated); break;
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