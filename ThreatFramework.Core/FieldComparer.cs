using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ThreatFramework.Core
{
    public static class FieldComparer
    {
        /// <summary>
        /// Compares two entities by a given set of property names (case-insensitive).
        /// Throws if any field is not found on the entity type.
        /// </summary>
        public static IReadOnlyList<FieldChange> CompareByNames<T>(
            T left, T right, IEnumerable<string> fieldNames) where T : class
        {
            if (left is null) throw new ArgumentNullException(nameof(left));
            if (right is null) throw new ArgumentNullException(nameof(right));

            var type = typeof(T);
            var requested = fieldNames?.ToArray() ?? Array.Empty<string>();
            if (requested.Length == 0) return Array.Empty<FieldChange>();

            // Validate: all fields must exist (throw on missing)
            var props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            var map = props.ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

            foreach (var f in requested)
            {
                if (!map.ContainsKey(f))
                    throw new MissingMemberException(type.FullName, f);
            }

            var changes = new List<FieldChange>(capacity: requested.Length);

            foreach (var f in requested)
            {
                var pi = map[f];
                var lv = pi.GetValue(left);
                var rv = pi.GetValue(right);

                if (!EqualsNormalized(lv, rv))
                {
                    // Replace the object initializer for FieldChange with a constructor call
                    changes.Add(new FieldChange(
                        pi.Name, // preserve actual casing
                        lv,
                        rv
                    ));
                }
            }

            return changes;
        }

        private static bool EqualsNormalized(object? a, object? b)
        {
            if (a is null && b is null) return true;
            if (a is null || b is null) return false;

            // Normalize sequences by value when both are IEnumerable and not string
            if (a is System.Collections.IEnumerable ea && b is System.Collections.IEnumerable eb &&
                a is not string && b is not string)
            {
                var la = ea.Cast<object?>().ToArray();
                var lb = eb.Cast<object?>().ToArray();
                if (la.Length != lb.Length) return false;
                for (int i = 0; i < la.Length; i++)
                    if (!Equals(la[i], lb[i])) return false;
                return true;
            }

            return Equals(a, b);
        }
    }
}
