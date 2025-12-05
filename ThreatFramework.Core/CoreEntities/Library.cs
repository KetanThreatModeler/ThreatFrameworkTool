using System;
using System.Collections.Generic;
using ThreatModeler.TF.Core.CustomException;
using ThreatModeler.TF.Core.Helper; // Using your specific helper namespace

namespace ThreatFramework.Core.CoreEntities
{
    public class Library : IFieldComparable<Library>
    {
        public Guid Guid { get; set; }
        public int DepartmentId { get; set; }
        public bool Readonly { get; set; }
        public bool IsDefault { get; set; }

        public string Name { get; set; }
        public string SharingType { get; set; }
        public string Description { get; set; }
        public string Labels { get; set; }
        public string Version { get; set; }
        public string ReleaseNotes { get; set; }
        public string ImageURL { get; set; }

        public List<FieldChange> CompareFields(Library other, IEnumerable<string> fields)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));

            var changes = new List<FieldChange>();

            foreach (var field in fields)
            {
                switch (field)
                {
                    // --- GROUP 1: Value Types (Guid, Int, Bool) ---
                    case nameof(Guid):
                    case nameof(DepartmentId):
                    case nameof(Readonly):
                    case nameof(IsDefault):
                        CompareValueTypes(changes, field, other);
                        break;

                    // --- GROUP 2: Case-Insensitive Strings ---
                    case nameof(Name):
                        ComparisonHelper.CompareString(changes, field, this.Name, other.Name, ignoreCase: true);
                        break;

                    // --- GROUP 3: Standard Strings (Case-Sensitive) ---
                    case nameof(SharingType):
                    case nameof(Description):
                    case nameof(Labels):
                    case nameof(Version):
                    case nameof(ReleaseNotes):
                    case nameof(ImageURL):
                        string? s1 = GetStringValue(field);
                        string? s2 = other.GetStringValue(field);
                        ComparisonHelper.CompareString(changes, field, s1, s2, ignoreCase: false);
                        break;

                    // --- ERROR HANDLING ---
                    default:
                        throw new FieldComparisonNotImplementedException(nameof(Library), field);
                }
            }

            return changes;
        }

        // --- Private Helper: Value Types ---
        private void CompareValueTypes(List<FieldChange> changes, string field, Library other)
        {
            switch (field)
            {
                case nameof(Guid): ComparisonHelper.Compare(changes, field, this.Guid, other.Guid); break;
                case nameof(DepartmentId): ComparisonHelper.Compare(changes, field, this.DepartmentId, other.DepartmentId); break;
                case nameof(Readonly): ComparisonHelper.Compare(changes, field, this.Readonly, other.Readonly); break;
                case nameof(IsDefault): ComparisonHelper.Compare(changes, field, this.IsDefault, other.IsDefault); break;
            }
        }

        // --- Private Helper: Strings ---
        private string? GetStringValue(string fieldName) => fieldName switch
        {
            nameof(SharingType) => SharingType,
            nameof(Description) => Description,
            nameof(Labels) => Labels,
            nameof(Version) => Version,
            nameof(ReleaseNotes) => ReleaseNotes,
            nameof(ImageURL) => ImageURL,
            _ => null
        };
    }
}