using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Core;

namespace ThreatModeler.TF.Core.Helper
{
    public static class ComparisonHelper
    {
        // 1. Generic Comparison (Int, Guid, Bool, standard String)
        public static void Compare<T>(List<FieldChange> changes, string fieldName, T val1, T val2)
        {
            if (!EqualityComparer<T>.Default.Equals(val1, val2))
            {
                changes.Add(new FieldChange(fieldName.ToLower(), val1, val2));
            }
        }

        // 2. String Comparison with explicit Case sensitivity option
        public static void CompareString(List<FieldChange> changes, string fieldName, string? val1, string? val2, bool ignoreCase = false)
        {
            var comparisonType = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

            // Handle nulls gracefully
            if (!string.Equals(val1, val2, comparisonType))
            {
                changes.Add(new FieldChange(fieldName.ToLower(), val1, val2));
            }
        }

        // 3. List Comparison
        public static void CompareList<T>(List<FieldChange> changes, string fieldName, List<T>? list1, List<T>? list2)
        {
            var l1 = list1 ?? new List<T>();
            var l2 = list2 ?? new List<T>();

            if (l1.Count != l2.Count || !l1.SequenceEqual(l2))
            {
                changes.Add(new FieldChange(fieldName.ToLower(), list1, list2));
            }
        }
    }
}