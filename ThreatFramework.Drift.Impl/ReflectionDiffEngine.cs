using System.Globalization;
using System.Reflection;
using ThreatFramework.Core;
using ThreatFramework.Drift.Contract;
using ThreatFramework.Drift.Contract.Model;
using ThreatFramework.Infrastructure;

namespace ThreatFramework.Drift.Impl
{
    public class ReflectionDiffEngine<T> : IDiffEngine<T>
    {
        private static readonly HashSet<string> DefaultExcludedFields = new(StringComparer.OrdinalIgnoreCase)
        {
            "Id", "Guid", "CreatedDate", "LastUpdated", "DateCreated", "LibraryId", "LibraryGuid"
        };

        public EntityDiff<T> Diff(
            IReadOnlyCollection<T> baseline,
            IReadOnlyCollection<T> target,
            Func<string, bool>? includeFieldPredicate = null)
        {
            // index by key
            var keySelector = new Func<T, string>(e => new ReflectionIdentityResolver().GetEntityKey(e));
            var baseMap = baseline.ToDictionary(keySelector, e => e, StringComparer.OrdinalIgnoreCase);
            var targMap = target.ToDictionary(keySelector, e => e, StringComparer.OrdinalIgnoreCase);

            var added = targMap.Keys.Except(baseMap.Keys, StringComparer.OrdinalIgnoreCase).Select(k => targMap[k]).ToList();
            var removed = baseMap.Keys.Except(targMap.Keys, StringComparer.OrdinalIgnoreCase).Select(k => baseMap[k]).ToList();

            var commonKeys = baseMap.Keys.Intersect(targMap.Keys, StringComparer.OrdinalIgnoreCase);
            var modified = new List<ModifiedEntity<T>>();

            foreach (var k in commonKeys)
            {
                var before = baseMap[k];
                var after = targMap[k];
                var changes = CompareFields(before, after, includeFieldPredicate);
                if (changes.Count > 0)
                {
                    modified.Add(new ModifiedEntity<T>
                    {
                        EntityKey = k,
                        ModifiedFields = changes
                    });
                }
            }

            return new EntityDiff<T>
            {
                Added = added,
                Removed = removed,
                Modified = modified
            };
        }

        private static List<FieldChange> CompareFields(T before, T after, Func<string, bool>? includeFieldPredicate)
        {
            var results = new List<FieldChange>();
            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                 .Where(p => p.CanRead)
                                 .Where(p => !DefaultExcludedFields.Contains(p.Name));

            if (includeFieldPredicate != null)
                props = props.Where(p => includeFieldPredicate(p.Name));

            foreach (var p in props)
            {
                var oldVal = p.GetValue(before);
                var newVal = p.GetValue(after);
                if (!AreEqual(oldVal, newVal))
                {
                    results.Add(new FieldChange(
                        p.Name,
                        ToStringInvariant(oldVal),
                        ToStringInvariant(newVal)));
                }
            }
            return results;
        }

        private static string? ToStringInvariant(object? v)
        {
            if (v is null) return null;
            return v switch
            {
                DateTime dt => dt.ToString("o", CultureInfo.InvariantCulture),
                DateTimeOffset dto => dto.ToString("o", CultureInfo.InvariantCulture),
                _ => Convert.ToString(v, CultureInfo.InvariantCulture)
            };
        }

        private static bool AreEqual(object? a, object? b)
        {
            if (a is null && b is null) return true;
            if (a is null || b is null) return false;
            if (a.Equals(b)) return true;
            // Normalize strings for whitespace differences
            if (a is string sa && b is string sb)
                return string.Equals(sa?.Trim(), sb?.Trim(), StringComparison.Ordinal);
            return false;
        }
    }
}
