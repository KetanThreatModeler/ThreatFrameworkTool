using ThreatFramework.Drift.Contract.CoreEntityDriftService;
using ThreatFramework.Drift.Contract.CoreEntityDriftService.Model;
using ThreatFramework.Drift.Contract.FolderDiff;

namespace ThreatFramework.Drift.Impl.CoreEntityDriftService
{
    public sealed class CoreEntityDrift : ICoreEntityDrift
    {
        private readonly IYamlReaderRouter _router;

        public CoreEntityDrift(IYamlReaderRouter router)
        {
            _router = router;
        }

        public async Task<CoreEntitiesDrift> BuildAsync(FolderComparisionResult request)
        {
            ValidateRequest(request);

            var report = new CoreEntitiesDrift();

            // Normalize and bucket file paths by entity folder
            var added = BucketByEntity(request.AddedFiles);
            var removed = BucketByEntity(request.RemovedFiles);
            var modified = BucketByEntity(request.ModifiedFiles);

            // ---- Added & Removed: batch per entity type for perf
            await Task.WhenAll(new Task[]
            {
            PopulateAddedAsync(report, added, request.TargetFolderPath),
            PopulateRemovedAsync(report, removed, request.BaseLineFolderPath)
            }).ConfigureAwait(false);

            // ---- Modified: read per file to pair {Existing=BaseLineFolderPath(ClientDB), Updated=TargetFolderPath(GoldenDB)} reliably
            await PopulateModifiedAsync(report, modified, request.BaseLineFolderPath, request.TargetFolderPath)
                .ConfigureAwait(false);

            return report;
        }

        private static void ValidateRequest(FolderComparisionResult request)
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
            CoreEntitiesDrift report,
            BucketedPaths bucketed,
            string rootPath)
        {
            // Generate absolute paths once
            var abs = bucketed.ToAbsolute(rootPath);

            var tasks = new List<Task>();


            if (abs.Libraries.Count > 0)
                tasks.Add(LoadAndAppendAsync(abs.Libraries,
                    files => _router.ReadLibrariesAsync(files),
                    items => report.Libraries.Added.AddRange(items)));

            if (abs.Threats.Count > 0)
                tasks.Add(LoadAndAppendAsync(abs.Threats,
                    files => _router.ReadThreatsAsync(files),
                    items => report.Threats.Added.AddRange(items)));

            if (abs.Components.Count > 0)
                tasks.Add(LoadAndAppendAsync(abs.Components,
                    files => _router.ReadComponentsAsync(files),
                    items => report.Components.Added.AddRange(items)));

            if (abs.SecurityRequirements.Count > 0)
                tasks.Add(LoadAndAppendAsync(abs.SecurityRequirements,
                    files => _router.ReadSecurityRequirementsAsync(files),
                    items => report.SecurityRequirements.Added.AddRange(items)));

            if (abs.TestCases.Count > 0)
                tasks.Add(LoadAndAppendAsync(abs.TestCases,
                    files => _router.ReadTestCasesAsync(files),
                    items => report.TestCases.Added.AddRange(items)));

            if (abs.Properties.Count > 0)
                tasks.Add(LoadAndAppendAsync(abs.Properties,
                    files => _router.ReadPropertiesAsync(files),
                    items => report.Properties.Added.AddRange(items)));

            if (abs.PropertyOptions.Count > 0)
                tasks.Add(LoadAndAppendAsync(abs.PropertyOptions,
                    files => _router.ReadPropertyOptionsAsync(files),
                    items => report.PropertyOptions.Added.AddRange(items)));

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        // --------------------------
        // Removed (read from Baseline)
        // --------------------------
        private async Task PopulateRemovedAsync(
            CoreEntitiesDrift report,
            BucketedPaths bucketed,
            string rootPath)
        {
            var abs = bucketed.ToAbsolute(rootPath);

            var tasks = new List<Task>();

            if (abs.Libraries.Count > 0)
                tasks.Add(LoadAndAppendAsync(abs.Libraries,
                    files => _router.ReadLibrariesAsync(files),
                    items => report.Libraries.Removed.AddRange(items)));

            if (abs.Components.Count > 0)
                tasks.Add(LoadAndAppendAsync(abs.Components,
                    files => _router.ReadComponentsAsync(files),
                    items => report.Components.Removed.AddRange(items)));

            if (abs.Threats.Count > 0)
                tasks.Add(LoadAndAppendAsync(abs.Threats,
                    files => _router.ReadThreatsAsync(files),
                    items => report.Threats.Removed.AddRange(items)));

            if(abs.SecurityRequirements.Count > 0)
                tasks.Add(LoadAndAppendAsync(abs.SecurityRequirements,
                    files => _router.ReadSecurityRequirementsAsync(files),
                    items => report.SecurityRequirements.Removed.AddRange(items)));

            if (abs.TestCases.Count > 0)
                tasks.Add(LoadAndAppendAsync(abs.TestCases,
                    files => _router.ReadTestCasesAsync(files),
                    items => report.TestCases.Removed.AddRange(items)));

            if (abs.Properties.Count > 0)
                tasks.Add(LoadAndAppendAsync(abs.Properties,
                    files => _router.ReadPropertiesAsync(files),
                    items => report.Properties.Removed.AddRange(items)));

            if (abs.PropertyOptions.Count > 0)
                tasks.Add(LoadAndAppendAsync(abs.PropertyOptions,
                    files => _router.ReadPropertyOptionsAsync(files),
                    items => report.PropertyOptions.Removed.AddRange(items)));

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        // -----------------------------------------
        // Modified (Existing=Target, Updated=Baseline)
        // Read per-file to maintain strict pairing
        // -----------------------------------------
        private async Task PopulateModifiedAsync(
            CoreEntitiesDrift report,
            BucketedPaths bucketed,
            string targetRoot,
            string baselineRoot)
        {

            // Libraries

            foreach (var rel in bucketed.Libraries)
            {
                var pair = await ReadPairAsync(rel,
                    filesTarget => _router.ReadLibrariesAsync(filesTarget),
                    filesBaseline => _router.ReadLibrariesAsync(filesBaseline),
                    targetRoot, baselineRoot).ConfigureAwait(false);
                if (pair is not null) report.Libraries.Modified.Add(pair);
            }

            // Components
            foreach (var rel in bucketed.Components)
            {
                var pair = await ReadPairAsync(rel,
                    filesTarget => _router.ReadComponentsAsync(filesTarget),
                    filesBaseline => _router.ReadComponentsAsync(filesBaseline),
                    targetRoot, baselineRoot).ConfigureAwait(false);

                if (pair is not null) report.Components.Modified.Add(pair);
            }

            // Threats
            foreach (var rel in bucketed.Threats)
            {
                var pair = await ReadPairAsync(rel,
                    filesTarget => _router.ReadThreatsAsync(filesTarget),
                    filesBaseline => _router.ReadThreatsAsync(filesBaseline),
                    targetRoot, baselineRoot).ConfigureAwait(false);

                if (pair is not null) report.Threats.Modified.Add(pair);
            }

            // SecurityRequirements
            foreach (var rel in bucketed.SecurityRequirements)
            {
                var pair = await ReadPairAsync(rel,
                    filesTarget => _router.ReadSecurityRequirementsAsync(filesTarget),
                    filesBaseline => _router.ReadSecurityRequirementsAsync(filesBaseline),
                    targetRoot, baselineRoot).ConfigureAwait(false);
                if (pair is not null) report.SecurityRequirements.Modified.Add(pair);
            }

            // TestCases
            foreach (var rel in bucketed.TestCases)
            {
                var pair = await ReadPairAsync(rel,
                    filesTarget => _router.ReadTestCasesAsync(filesTarget),
                    filesBaseline => _router.ReadTestCasesAsync(filesBaseline),
                    targetRoot, baselineRoot).ConfigureAwait(false);

                if (pair is not null) report.TestCases.Modified.Add(pair);
            }

            // Properties
            foreach (var rel in bucketed.Properties)
            {
                var pair = await ReadPairAsync(rel,
                    filesTarget => _router.ReadPropertiesAsync(filesTarget),
                    filesBaseline => _router.ReadPropertiesAsync(filesBaseline),
                    targetRoot, baselineRoot).ConfigureAwait(false);

                if (pair is not null) report.Properties.Modified.Add(pair);
            }

            // PropertyOptions
            foreach (var rel in bucketed.PropertyOptions)
            {
                var pair = await ReadPairAsync(rel,
                    filesTarget => _router.ReadPropertyOptionsAsync(filesTarget),
                    filesBaseline => _router.ReadPropertyOptionsAsync(filesBaseline),
                    targetRoot, baselineRoot).ConfigureAwait(false);

                if (pair is not null) report.PropertyOptions.Modified.Add(pair);
            }
        }

        // --------- helpers ----------

        private static async Task LoadAndAppendAsync<T>(
            List<string> absolutePaths,
            Func<IEnumerable<string>, Task<IEnumerable<T>>> readAsync,
            Action<IEnumerable<T>> append)
            where T : class
        {
            var items = await readAsync(absolutePaths).ConfigureAwait(false);
            append(items);
        }

        private static async Task<EntityPair<T>?> ReadPairAsync<T>(
            string relativePath,
            Func<IEnumerable<string>, Task<IEnumerable<T>>> readFromTarget,
            Func<IEnumerable<string>, Task<IEnumerable<T>>> readFromBaseline,
            string targetRoot, string baselineRoot)
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

            return new EntityPair<T> { Existing = existing, Updated = updated };
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
                    case "libraries": bucket.Libraries.Add(rel); break;
                    case "threats": bucket.Threats.Add(rel); break;
                    case "components": bucket.Components.Add(rel); break;
                    case "security-requirements": bucket.SecurityRequirements.Add(rel); break;
                    case "test-cases": bucket.TestCases.Add(rel); break;
                    case "properties": bucket.Properties.Add(rel); break;
                    case "propertyoptions": bucket.PropertyOptions.Add(rel); break;
                    default:
                        // Unknown folder; ignore silently or collect to bucket.Unknown if you want to report
                        break;
                }
            }
            return bucket;
        }

        private sealed class BucketedPaths
        {
            public List<string> Libraries { get; } = new();
            public List<string> Components { get; } = new();
            public List<string> Threats { get; } = new();
            public List<string> SecurityRequirements { get; } = new();
            public List<string> TestCases { get; } = new();
            public List<string> Properties { get; } = new();
            public List<string> PropertyOptions { get; } = new();

            public AbsolutePaths ToAbsolute(string root)
            {
                static List<string> Map(string root, IEnumerable<string> rels)
                    => rels.Select(r => Path.Combine(root, r)).ToList();

                return new AbsolutePaths
                {
                    Libraries = Map(root, Libraries),
                    Components = Map(root, Components),
                    Threats = Map(root, Threats),
                    SecurityRequirements = Map(root, SecurityRequirements),
                    TestCases = Map(root, TestCases),
                    Properties = Map(root, Properties),
                    PropertyOptions = Map(root, PropertyOptions),
                };
            }
        }

        private sealed class AbsolutePaths
        {
            public List<string> Libraries { get; init; } = new();
            public List<string> Components { get; init; } = new();
            public List<string> Threats { get; init; } = new();
            public List<string> SecurityRequirements { get; init; } = new();
            public List<string> TestCases { get; init; } = new();
            public List<string> Properties { get; init; } = new();
            public List<string> PropertyOptions { get; init; } = new();
        }
    }
}
