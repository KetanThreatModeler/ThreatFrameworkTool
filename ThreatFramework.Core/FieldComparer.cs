using System.Reflection;

namespace ThreatFramework.Core
{
    public static class FieldComparer
    {
        /// <summary>
        /// Compares two entities by a given set of property names (case-insensitive).
        /// Throws if any field is not found on the entity type.
        /// </summary>
        public static List<FieldChange> CompareByNames<T>(
            T left, T right, IEnumerable<string> fieldNames) where T : class
        {
            if (left is null)
            {
                throw new ArgumentNullException(nameof(left));
            }

            if (right is null)
            {
                throw new ArgumentNullException(nameof(right));
            }

            Type type = typeof(T);
            string[] requested = fieldNames?.ToArray() ?? Array.Empty<string>();
            if (requested.Length == 0)
            {
                return [];
            }

            // Validate: all fields must exist (throw on missing)
            PropertyInfo[] props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            Dictionary<string, PropertyInfo> map = props.ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

            foreach (string? f in requested)
            {
                if (!map.ContainsKey(f))
                {
                    throw new MissingMemberException(type.FullName, f);
                }
            }

            List<FieldChange> changes = new(capacity: requested.Length);

            foreach (string? f in requested)
            {
                PropertyInfo pi = map[f];
                object? lv = pi.GetValue(left);
                object? rv = pi.GetValue(right);

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
            if (a is null && b is null)
            {
                return true;
            }

            if (a is null || b is null)
            {
                return false;
            }

            // Check for IsOverridden field - if true on either object, consider them equal
            if (HasIsOverriddenTrue(a) || HasIsOverriddenTrue(b))
            {
                return true;
            }

            // Normalize sequences by value when both are IEnumerable and not string
            if (a is System.Collections.IEnumerable ea && b is System.Collections.IEnumerable eb &&
                a is not string && b is not string)
            {
                object?[] la = ea.Cast<object?>().ToArray();
                object?[] lb = eb.Cast<object?>().ToArray();
                if (la.Length != lb.Length)
                {
                    return false;
                }

                for (int i = 0; i < la.Length; i++)
                {
                    if (!Equals(la[i], lb[i]))
                    {
                        return false;
                    }
                }

                return true;
            }

            return Equals(a, b);
        }

        private static bool HasIsOverriddenTrue(object? obj)
        {
            if (obj is null)
            {
                return false;
            }

            Type type = obj.GetType();
            PropertyInfo? prop = type.GetProperty("IsOverridden", BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);

            return prop?.PropertyType == typeof(bool) && (bool)(prop.GetValue(obj) ?? false);
        }
    }
}
