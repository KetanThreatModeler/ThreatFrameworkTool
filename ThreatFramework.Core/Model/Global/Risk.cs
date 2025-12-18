using ThreatFramework.Core;
using ThreatModeler.TF.Core.CustomException;
using ThreatModeler.TF.Core.Helper;

namespace ThreatModeler.TF.Core.Model.Global
{
    public sealed class Risk : IFieldComparable<Risk>
    {
        public string Name { get; set; } = string.Empty;
        public required string Color { get; set; }
        public required string SuggestedName { get; set; }
        public int Score { get; set; }
        public required string ChineseName { get; set; }

        public List<FieldChange> CompareFields(Risk other, IEnumerable<string> fields)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));

            var changes = new List<FieldChange>();

            foreach (var field in fields)
            {
                switch (field)
                {
                    // --- GROUP 1: Value Types ---
                    case nameof(Score):
                        ComparisonHelper.Compare(changes, field, Score, other.Score);
                        break;

                    // --- GROUP 2: Case-Insensitive Strings (Name-like) ---
                    case nameof(Name):
                        ComparisonHelper.CompareString(
                            changes,
                            field,
                            Name,
                            other.Name,
                            ignoreCase: true);
                        break;

                    case nameof(SuggestedName):
                        ComparisonHelper.CompareString(
                            changes,
                            field,
                            SuggestedName,
                            other.SuggestedName,
                            ignoreCase: true);
                        break;

                    // --- GROUP 3: Standard Strings (Case-Sensitive) ---
                    case nameof(Color):
                    case nameof(ChineseName):
                        string? s1 = GetStringValue(field);
                        string? s2 = other.GetStringValue(field);
                        ComparisonHelper.CompareString(
                            changes,
                            field,
                            s1,
                            s2,
                            ignoreCase: false);
                        break;

                    // --- ERROR HANDLING ---
                    default:
                        throw new FieldComparisonNotImplementedException(nameof(Risk), field);
                }
            }

            return changes;
        }

        // --- Private Helper: Strings ---
        private string? GetStringValue(string fieldName) => fieldName switch
        {
            nameof(Color) => Color,
            nameof(ChineseName) => ChineseName,
            _ => null
        };
    }
}
