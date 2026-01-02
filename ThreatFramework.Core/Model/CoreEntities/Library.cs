using ThreatFramework.Core;
using ThreatModeler.TF.Core.CustomException;
using ThreatModeler.TF.Core.Helper;

namespace ThreatModeler.TF.Core.Model.CoreEntities
{
    public class Library : IFieldComparable<Library>
    {
        public Guid Guid { get; set; }
        public int DepartmentId { get; set; }
        public bool Readonly { get; set; }
        public bool IsDefault { get; set; }

        public string Name { get; set; } = string.Empty;
        public string SharingType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Labels { get; set; } = new List<string>();
        public string Version { get; set; } = string.Empty;
        public string ReleaseNotes { get; set; } = string.Empty;
        public string ImageURL { get; set; } = string.Empty;

        public List<FieldChange> CompareFields(Library other, IEnumerable<string> fields)
        {
            if (other is null) throw new ArgumentNullException(nameof(other));
            if (fields is null) throw new ArgumentNullException(nameof(fields));

            var changes = new List<FieldChange>();

            foreach (var rawField in fields)
            {
                if (string.IsNullOrWhiteSpace(rawField))
                    continue;

                var key = rawField.Trim().ToLowerInvariant();

                switch (key)
                {
                    // --- GROUP 1: Value Types (Guid, Int, Bool) ---
                    case "guid":
                        ComparisonHelper.Compare(changes, nameof(Guid), Guid, other.Guid);
                        break;

                    case "departmentid":
                        ComparisonHelper.Compare(changes, nameof(DepartmentId), DepartmentId, other.DepartmentId);
                        break;

                    case "readonly":
                        ComparisonHelper.Compare(changes, nameof(Readonly), Readonly, other.Readonly);
                        break;

                    case "isdefault":
                        ComparisonHelper.Compare(changes, nameof(IsDefault), IsDefault, other.IsDefault);
                        break;

                    // --- GROUP 2: Case-Insensitive Strings ---
                    case "name":
                        ComparisonHelper.CompareString(changes, nameof(Name), Name, other.Name, ignoreCase: true);
                        break;

                    // --- GROUP 3: Case-Sensitive Strings ---
                    case "sharingtype":
                        ComparisonHelper.CompareString(changes, nameof(SharingType), SharingType, other.SharingType, ignoreCase: false);
                        break;

                    case "description":
                        ComparisonHelper.CompareString(changes, nameof(Description), Description, other.Description, ignoreCase: false);
                        break;

                    case "labels":
                        ComparisonHelper.CompareList(changes, nameof(Labels), Labels, other.Labels);
                        break;

                    case "version":
                        ComparisonHelper.CompareString(changes, nameof(Version), Version, other.Version, ignoreCase: false);
                        break;

                    case "releasenotes":
                    case "releasenote": // allow singular key from YAML
                        ComparisonHelper.CompareString(changes, nameof(ReleaseNotes), ReleaseNotes, other.ReleaseNotes, ignoreCase: false);
                        break;

                    case "imageurl":
                    case "imageurl ":
                    case "image_url":
                    case "image":
                        ComparisonHelper.CompareString(changes, nameof(ImageURL), ImageURL, other.ImageURL, ignoreCase: false);
                        break;

                    default:
                        // Keep original raw field in message for easier debugging
                        throw new FieldComparisonNotImplementedException(nameof(Library), rawField);
                }
            }

            return changes;
        }
    }
}
