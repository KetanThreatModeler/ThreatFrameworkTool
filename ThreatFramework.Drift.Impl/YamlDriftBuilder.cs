using ThreatFramework.Drift.Contract;
using ThreatFramework.Drift.Contract.Model;

namespace ThreatFramework.Drift.Impl
{
    public sealed class YamlDriftBuilder : IYamlDriftBuilder
    {
        private readonly IYamlReaderRouter _router;

        public YamlDriftBuilder(IYamlReaderRouter router)
        {
            _router = router;
        }

        public async Task<EntityDriftReport> BuildAsync(YamlFilesDriftReport request, CancellationToken cancellationToken = default)
        {
            ValidateRequest(request);

            var report = new EntityDriftReport();

            // Normalize and bucket file paths by entity folder
            var added = BucketByEntity(request.AddedFiles);
            var removed = BucketByEntity(request.RemovedFiles);
            var modified = BucketByEntity(request.ModifiedFiles);

            // ---- Added & Removed: batch per entity type for perf
            await Task.WhenAll(new Task[]
            {
            PopulateAddedAsync(report, added, request.BaseLineFolderPath, cancellationToken),
            PopulateRemovedAsync(report, removed, request.TargetFolderPath, cancellationToken)
            }).ConfigureAwait(false);

            // ---- Modified: read per file to pair {Existing=Target, Updated=Baseline} reliably
            await PopulateModifiedAsync(report, modified, request.TargetFolderPath, request.BaseLineFolderPath, cancellationToken)
                .ConfigureAwait(false);

            return report;
        }

        private static void ValidateRequest(YamlFilesDriftReport request)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(request.BaseLineFolderPath))
                throw new ArgumentException("Baseline folder path is required.", nameof(request.BaseLineFolderPath));
            if (string.IsNullOrWhiteSpace(request.TargetFolderPath))
                throw new ArgumentException("Target folder path is required.", nameof(request.TargetFolderPath));
        }

        // ------------------------
        // Added (read from Target)
        // ------------------------
        private async Task PopulateAddedAsync(
            EntityDriftReport report,
            BucketedPaths bucketed,
            string rootPath,
            CancellationToken ct)
        {
            // Generate absolute paths once
            var abs = bucketed.ToAbsolute(rootPath);

            var tasks = new List<Task>();

            if (abs.Threats.Count > 0)
                tasks.Add(LoadAndAppendAsync(abs.Threats, ct,
                    files => _router.ReadThreatsAsync(files, ct),
                    items => report.Threats.Added.AddRange(items)));

            if (abs.Components.Count > 0)
                tasks.Add(LoadAndAppendAsync(abs.Components, ct,
                    files => _router.ReadComponentsAsync(files, ct),
                    items => report.Components.Added.AddRange(items)));

            if (abs.TestCases.Count > 0)
                tasks.Add(LoadAndAppendAsync(abs.TestCases, ct,
                    files => _router.ReadTestCasesAsync(files, ct),
                    items => report.TestCases.Added.AddRange(items)));

            if (abs.Properties.Count > 0)
                tasks.Add(LoadAndAppendAsync(abs.Properties, ct,
                    files => _router.ReadPropertiesAsync(files, ct),
                    items => report.Properties.Added.AddRange(items)));

            if (abs.PropertyOptions.Count > 0)
                tasks.Add(LoadAndAppendAsync(abs.PropertyOptions, ct,
                    files => _router.ReadPropertyOptionsAsync(files, ct),
                    items => report.PropertyOptions.Added.AddRange(items)));

            if (abs.Libraries.Count > 0)
                tasks.Add(LoadAndAppendAsync(abs.Libraries, ct,
                    files => _router.ReadLibrariesAsync(files, ct),
                    items => report.Libraries.Added.AddRange(items)));

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        // --------------------------
        // Removed (read from Baseline)
        // --------------------------
        private async Task PopulateRemovedAsync(
            EntityDriftReport report,
            BucketedPaths bucketed,
            string rootPath,
            CancellationToken ct)
        {
            var abs = bucketed.ToAbsolute(rootPath);

            var tasks = new List<Task>();

            if (abs.Threats.Count > 0)
                tasks.Add(LoadAndAppendAsync(abs.Threats, ct,
                    files => _router.ReadThreatsAsync(files, ct),
                    items => report.Threats.Removed.AddRange(items)));

            if (abs.Components.Count > 0)
                tasks.Add(LoadAndAppendAsync(abs.Components, ct,
                    files => _router.ReadComponentsAsync(files, ct),
                    items => report.Components.Removed.AddRange(items)));

            if (abs.TestCases.Count > 0)
                tasks.Add(LoadAndAppendAsync(abs.TestCases, ct,
                    files => _router.ReadTestCasesAsync(files, ct),
                    items => report.TestCases.Removed.AddRange(items)));

            if (abs.Properties.Count > 0)
                tasks.Add(LoadAndAppendAsync(abs.Properties, ct,
                    files => _router.ReadPropertiesAsync(files, ct),
                    items => report.Properties.Removed.AddRange(items)));

            if (abs.PropertyOptions.Count > 0)
                tasks.Add(LoadAndAppendAsync(abs.PropertyOptions, ct,
                    files => _router.ReadPropertyOptionsAsync(files, ct),
                    items => report.PropertyOptions.Removed.AddRange(items)));

            if (abs.Libraries.Count > 0)
                tasks.Add(LoadAndAppendAsync(abs.Libraries, ct,
                    files => _router.ReadLibrariesAsync(files, ct),
                    items => report.Libraries.Removed.AddRange(items)));

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        // -----------------------------------------
        // Modified (Existing=Target, Updated=Baseline)
        // Read per-file to maintain strict pairing
        // -----------------------------------------
        private async Task PopulateModifiedAsync(
            EntityDriftReport report,
            BucketedPaths bucketed,
            string targetRoot,
            string baselineRoot,
            CancellationToken ct)
        {
            // Threats
            foreach (var rel in bucketed.Threats)
            {
                var pair = await ReadPairAsync(rel,
                    filesTarget => _router.ReadThreatsAsync(filesTarget, ct),
                    filesBaseline => _router.ReadThreatsAsync(filesBaseline, ct),
                    targetRoot, baselineRoot, ct).ConfigureAwait(false);

                if (pair is not null) report.Threats.Modified.Add(pair);
            }

            // Components
            foreach (var rel in bucketed.Components)
            {
                var pair = await ReadPairAsync(rel,
                    filesTarget => _router.ReadComponentsAsync(filesTarget, ct),
                    filesBaseline => _router.ReadComponentsAsync(filesBaseline, ct),
                    targetRoot, baselineRoot, ct).ConfigureAwait(false);

                if (pair is not null) report.Components.Modified.Add(pair);
            }

            // TestCases
            foreach (var rel in bucketed.TestCases)
            {
                var pair = await ReadPairAsync(rel,
                    filesTarget => _router.ReadTestCasesAsync(filesTarget, ct),
                    filesBaseline => _router.ReadTestCasesAsync(filesBaseline, ct),
                    targetRoot, baselineRoot, ct).ConfigureAwait(false);

                if (pair is not null) report.TestCases.Modified.Add(pair);
            }

            // Properties
            foreach (var rel in bucketed.Properties)
            {
                var pair = await ReadPairAsync(rel,
                    filesTarget => _router.ReadPropertiesAsync(filesTarget, ct),
                    filesBaseline => _router.ReadPropertiesAsync(filesBaseline, ct),
                    targetRoot, baselineRoot, ct).ConfigureAwait(false);

                if (pair is not null) report.Properties.Modified.Add(pair);
            }

            // PropertyOptions
            foreach (var rel in bucketed.PropertyOptions)
            {
                var pair = await ReadPairAsync(rel,
                    filesTarget => _router.ReadPropertyOptionsAsync(filesTarget, ct),
                    filesBaseline => _router.ReadPropertyOptionsAsync(filesBaseline, ct),
                    targetRoot, baselineRoot, ct).ConfigureAwait(false);

                if (pair is not null) report.PropertyOptions.Modified.Add(pair);
            }

            // Libraries
            foreach (var rel in bucketed.Libraries)
            {
                var pair = await ReadPairAsync(rel,
                    filesTarget => _router.ReadLibrariesAsync(filesTarget, ct),
                    filesBaseline => _router.ReadLibrariesAsync(filesBaseline, ct),
                    targetRoot, baselineRoot, ct).ConfigureAwait(false);

                if (pair is not null) report.Libraries.Modified.Add(pair);
            }
        }

        // --------- helpers ----------

        private static async Task LoadAndAppendAsync<T>(
            IReadOnlyList<string> absolutePaths,
            CancellationToken ct,
            Func<IEnumerable<string>, Task<IEnumerable<T>>> readAsync,
            Action<IEnumerable<T>> append)
            where T : class
        {
            var items = await readAsync(absolutePaths).ConfigureAwait(false);
            append(items);
        }

        private static async Task<EntityDriftPair<T>?> ReadPairAsync<T>(
            string relativePath,
            Func<IEnumerable<string>, Task<IEnumerable<T>>> readFromTarget,
            Func<IEnumerable<string>, Task<IEnumerable<T>>> readFromBaseline,
            string targetRoot, string baselineRoot,
            CancellationToken ct)
            where T : class
        {
            var targetFull = Path.Combine(targetRoot, relativePath);
            var baselineFull = Path.Combine(baselineRoot, relativePath);

            // fail-safe: if either side is missing, skip (or you can throw/log)
            if (!File.Exists(targetFull) || !File.Exists(baselineFull))
                return null;

            var existingTask = readFromTarget(new[] { targetFull });
            var updatedTask = readFromBaseline(new[] { baselineFull });

            await Task.WhenAll(existingTask, updatedTask).ConfigureAwait(false);

            var existing = (await existingTask.ConfigureAwait(false)).FirstOrDefault();
            var updated = (await updatedTask.ConfigureAwait(false)).FirstOrDefault();

            if (existing is null || updated is null)
                return null;

            return new EntityDriftPair<T> { Existing = existing, Updated = updated };
        }

        private static BucketedPaths BucketByEntity(IEnumerable<string> relativePaths)
        {
            var bucket = new BucketedPaths();
            foreach (var rel in relativePaths ?? Enumerable.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(rel)) continue;

                var first = rel.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries)
                               .FirstOrDefault()?.Trim().ToLowerInvariant();
                if (string.IsNullOrEmpty(first)) continue;

                switch (first)
                {
                    case "threats": bucket.Threats.Add(rel); break;
                    case "components": bucket.Components.Add(rel); break;
                    case "testcases": bucket.TestCases.Add(rel); break;
                    case "properties": bucket.Properties.Add(rel); break;
                    case "propertyoptions": bucket.PropertyOptions.Add(rel); break;
                    case "libraries": bucket.Libraries.Add(rel); break;
                    default:
                        // Unknown folder; ignore silently or collect to bucket.Unknown if you want to report
                        break;
                }
            }
            return bucket;
        }

        private sealed class BucketedPaths
        {
            public List<string> Threats { get; } = new();
            public List<string> Components { get; } = new();
            public List<string> TestCases { get; } = new();
            public List<string> Properties { get; } = new();
            public List<string> PropertyOptions { get; } = new();
            public List<string> Libraries { get; } = new();

            public AbsolutePaths ToAbsolute(string root)
            {
                static List<string> Map(string root, IEnumerable<string> rels)
                    => rels.Select(r => Path.Combine(root, r)).ToList();

                return new AbsolutePaths
                {
                    Threats = Map(root, Threats),
                    Components = Map(root, Components),
                    TestCases = Map(root, TestCases),
                    Properties = Map(root, Properties),
                    PropertyOptions = Map(root, PropertyOptions),
                    Libraries = Map(root, Libraries)
                };
            }
        }

        private sealed class AbsolutePaths
        {
            public List<string> Threats { get; init; } = new();
            public List<string> Components { get; init; } = new();
            public List<string> TestCases { get; init; } = new();
            public List<string> Properties { get; init; } = new();
            public List<string> PropertyOptions { get; init; } = new();
            public List<string> Libraries { get; init; } = new();
        }
    }
}
