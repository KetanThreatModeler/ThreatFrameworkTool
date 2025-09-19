using System.Collections.Concurrent;
using ThreatFramework.Core;
using ThreatFramework.Core.CoreEntities;
using ThreatFramework.Drift.Contract;
using ThreatFramework.Drift.Contract.Model;

namespace ThreatFramework.Drift.Impl
{
    public sealed class LibraryDriftAggregator : ILibraryDriftAggregator
    {
        private readonly EntityDriftAggregationOptions _defaults;

        public LibraryDriftAggregator(EntityDriftAggregationOptions? defaults = null)
        {
            _defaults = defaults ?? new EntityDriftAggregationOptions();
        }

        public Task<IReadOnlyList<LibraryDrift>> AggregateAsync(
            EntityDriftReport report,
            EntityDriftAggregationOptions? options = null,
            IEnumerable<string>? libraryIds = null,
            IEnumerable<string>? threatFields = null,
            IEnumerable<string>? componentFields = null,
            IEnumerable<string>? securityRequirementFields = null,
            IEnumerable<string>? testCaseFields = null,
            IEnumerable<string>? propertyFields = null,
            CancellationToken cancellationToken = default)
        {
            if (report is null) throw new ArgumentNullException(nameof(report));
            var cfg = options ?? _defaults;

            // Bucket holder keyed by library
            var byLib = new ConcurrentDictionary<(Guid id, string name), LibraryDrift>(
                EqualityComparer<(Guid, string)>.Default);

            // 1) Added / Removed — grouped by library (fast paths)
            GroupByLibrary(report.Threats.Added, GetOrCreate(byLib)).ToList()
                .ForEach(p => p.lib.Threats.Added.AddRange(p.items));
            GroupByLibrary(report.Threats.Removed, GetOrCreate(byLib)).ToList()
                .ForEach(p => p.lib.Threats.Removed.AddRange(p.items));

            GroupByLibrary(report.Components.Added, GetOrCreate(byLib)).ToList()
                .ForEach(p => p.lib.Components.Added.AddRange(p.items));
            GroupByLibrary(report.Components.Removed, GetOrCreate(byLib)).ToList()
                .ForEach(p => p.lib.Components.Removed.AddRange(p.items));

            GroupByLibrary(report.SecurityRequirements.Added, GetOrCreate(byLib)).ToList()
                .ForEach(p => p.lib.SecurityRequirements.Added.AddRange(p.items));
            GroupByLibrary(report.SecurityRequirements.Removed, GetOrCreate(byLib)).ToList()
                .ForEach(p => p.lib.SecurityRequirements.Removed.AddRange(p.items));

            GroupByLibrary(report.TestCases.Added, GetOrCreate(byLib)).ToList()
                .ForEach(p => p.lib.TestCases.Added.AddRange(p.items));
            GroupByLibrary(report.TestCases.Removed, GetOrCreate(byLib)).ToList()
                .ForEach(p => p.lib.TestCases.Removed.AddRange(p.items));

            GroupByLibrary(report.Properties.Added, GetOrCreate(byLib)).ToList()
                .ForEach(p => p.lib.Properties.Added.AddRange(p.items));
            GroupByLibrary(report.Properties.Removed, GetOrCreate(byLib)).ToList()
                .ForEach(p => p.lib.Properties.Removed.AddRange(p.items));

            // 2) Modified — compute ModifiedFields per entity with entity- or reflection-based compare
            var threatCompareFields = (threatFields ?? cfg.ThreatDefaultFields).ToArray();
            var componentCompareFields = (componentFields ?? cfg.ComponentDefaultFields).ToArray();
            var secReqCompareFields = (securityRequirementFields ?? cfg.SecurityRequirementDefaultFields).ToArray();
            var testCaseCompareFields = (testCaseFields ?? cfg.TestCaseDefaultFields).ToArray();
            var propertyCompareFields = (propertyFields ?? cfg.PropertyDefaultFields).ToArray();

            // Threats
            foreach (var pair in report.Threats.Modified)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var lib = GetOrCreate(byLib)(LibraryKey.From(pair.Updated)); // group by Updated (Baseline) is fine; library GUID should be stable
                var changes = Compare(pair.Existing, pair.Updated, threatCompareFields);
                if (changes.Count == 0) continue;

                lib.Threats.Modified.Add(new ModifiedEntity<Threat>
                {
                    EntityKey = EntityKey.Of(pair.Updated),
                    ModifiedFields = changes
                });
            }

            // Components
            foreach (var pair in report.Components.Modified)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var lib = GetOrCreate(byLib)(LibraryKey.From(pair.Updated));
                var changes = Compare(pair.Existing, pair.Updated, componentCompareFields);
                if (changes.Count == 0) continue;

                lib.Components.Modified.Add(new ModifiedEntity<Component>
                {
                    EntityKey = EntityKey.Of(pair.Updated),
                    ModifiedFields = changes
                });
            }

            // Security Requirements
            foreach (var pair in report.SecurityRequirements.Modified)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var lib = GetOrCreate(byLib)(LibraryKey.From(pair.Updated));
                var changes = Compare(pair.Existing, pair.Updated, secReqCompareFields);
                if (changes.Count == 0) continue;

                lib.SecurityRequirements.Modified.Add(new ModifiedEntity<SecurityRequirement>
                {
                    EntityKey = EntityKey.Of(pair.Updated),
                    ModifiedFields = changes
                });
            }

            // Test Cases
            foreach (var pair in report.TestCases.Modified)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var lib = GetOrCreate(byLib)(LibraryKey.From(pair.Updated));
                var changes = Compare(pair.Existing, pair.Updated, testCaseCompareFields);
                if (changes.Count == 0) continue;

                lib.TestCases.Modified.Add(new ModifiedEntity<TestCase>
                {
                    EntityKey = EntityKey.Of(pair.Updated),
                    ModifiedFields = changes
                });
            }

            // Properties
            foreach (var pair in report.Properties.Modified)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var lib = GetOrCreate(byLib)(LibraryKey.From(pair.Updated));
                var changes = Compare(pair.Existing, pair.Updated, propertyCompareFields);
                if (changes.Count == 0) continue;

                lib.Properties.Modified.Add(new ModifiedEntity<Property>
                {
                    EntityKey = EntityKey.Of(pair.Updated),
                    ModifiedFields = changes
                });
            }

            var result = byLib.Values
                .OrderBy(x => x.LibraryName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.LibraryGuid)
                .ToList()
                .AsReadOnly();

            return Task.FromResult<IReadOnlyList<LibraryDrift>>(result);
        }

        // ---------------- helpers ----------------

        private static Func<LibraryKey, LibraryDrift> GetOrCreate(
            ConcurrentDictionary<(Guid id, string name), LibraryDrift> bucket)
            => key =>
            {
                var k = (key.Guid, key.Name);
                return bucket.GetOrAdd(k, _ => new LibraryDrift
                {
                    LibraryGuid = key.Guid,
                    LibraryName = key.Name
                });
            };

        private static IEnumerable<(LibraryDrift lib, List<T> items)> GroupByLibrary<T>(
            IEnumerable<T> items,
            Func<LibraryKey, LibraryDrift> getOrCreate)
            where T : class
        {
            // Group items by their library
            var groups = items
                .GroupBy(e => LibraryKey.From(e))
                .Select(g => (lib: getOrCreate(g.Key), items: g.ToList()));
            return groups;
        }

        private static List<FieldChange> Compare<T>(T existing, T updated, IEnumerable<string> fields)
            where T : class
        {
            if (existing is IFieldComparable<T> cmp1)
                return cmp1.CompareFields(updated, fields).ToList();

            if (updated is IFieldComparable<T> cmp2)
                return cmp2.CompareFields(existing, fields).ToList(); // same semantics: compare sets

            // Fallback: strict reflection-based compare
            return FieldComparer.CompareByNames(existing, updated, fields).ToList();
        }
    }
}
