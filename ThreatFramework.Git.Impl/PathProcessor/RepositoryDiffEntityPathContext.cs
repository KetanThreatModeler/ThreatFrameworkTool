using ThreatModeler.TF.Git.Contract.Models;
using ThreatModeler.TF.Git.Contract.PathProcessor;

namespace ThreatModeler.TF.Git.Implementation.PathProcessor
{
    /// <summary>
    /// Diff-specific context that exposes entity-level file changes
    /// using absolute disk paths for added, deleted, and modified files.
    /// </summary>
    internal sealed class RepositoryDiffEntityPathContext : IRepositoryDiffEntityPathContext
    {
        private readonly IPathClassifier _pathClassifier;
        private readonly FolderDiffReport _diff;

        public RepositoryDiffEntityPathContext(IPathClassifier pathClassifier, FolderDiffReport diff)
        {
            _pathClassifier = pathClassifier ?? throw new ArgumentNullException(nameof(pathClassifier));
            _diff = diff ?? throw new ArgumentNullException(nameof(diff));
        }

        #region Public API – Library definition

        public EntityFileChangeSet GetLibraryFileChanges() =>
            GetAggregatedLibraryEntityFileChanges(DomainEntityType.Library);

        public IReadOnlyCollection<LibraryEntityFileChanges> GetLibraryFileChangesByLibrary() =>
            GetLibraryEntityFileChanges(DomainEntityType.Library);

        #endregion

        #region Public API – Library-specific entities (aggregated)

        public EntityFileChangeSet GetComponentFileChanges() =>
            GetAggregatedLibraryEntityFileChanges(DomainEntityType.Components);

        public EntityFileChangeSet GetSecurityRequirementFileChanges() =>
            GetAggregatedLibraryEntityFileChanges(DomainEntityType.SecurityRequirements);

        public EntityFileChangeSet GetTestCaseFileChanges() =>
            GetAggregatedLibraryEntityFileChanges(DomainEntityType.TestCases);

        public EntityFileChangeSet GetThreatFileChanges() =>
            GetAggregatedLibraryEntityFileChanges(DomainEntityType.Threats);

        public EntityFileChangeSet GetPropertyFileChanges() =>
            GetAggregatedLibraryEntityFileChanges(DomainEntityType.Properties);

        #endregion

        #region Public API – Library-specific entities (library-wise)

        public IReadOnlyCollection<LibraryEntityFileChanges> GetComponentFileChangesByLibrary() =>
            GetLibraryEntityFileChanges(DomainEntityType.Components);

        public IReadOnlyCollection<LibraryEntityFileChanges> GetSecurityRequirementFileChangesByLibrary() =>
            GetLibraryEntityFileChanges(DomainEntityType.SecurityRequirements);

        public IReadOnlyCollection<LibraryEntityFileChanges> GetTestCaseFileChangesByLibrary() =>
            GetLibraryEntityFileChanges(DomainEntityType.TestCases);

        public IReadOnlyCollection<LibraryEntityFileChanges> GetThreatFileChangesByLibrary() =>
            GetLibraryEntityFileChanges(DomainEntityType.Threats);

        public IReadOnlyCollection<LibraryEntityFileChanges> GetPropertyFileChangesByLibrary() =>
            GetLibraryEntityFileChanges(DomainEntityType.Properties);

        #endregion

        #region Public API – Global entities

        public EntityFileChangeSet GetComponentTypeFileChanges() =>
            GetGlobalEntityFileChanges(DomainEntityType.ComponentType);

        public EntityFileChangeSet GetPropertyTypeFileChanges() =>
            GetGlobalEntityFileChanges(DomainEntityType.PropertyType);

        public EntityFileChangeSet GetPropertyOptionsFileChanges() =>
            GetGlobalEntityFileChanges(DomainEntityType.PropertyOptions);

        #endregion

        #region Public API – Mapping entities

        public EntityFileChangeSet GetComponentPropertyMappingFileChanges() =>
            GetGlobalEntityFileChanges(DomainEntityType.ComponentProperty);

        public EntityFileChangeSet GetComponentPropertyOptionsMappingFileChanges() =>
            GetGlobalEntityFileChanges(DomainEntityType.ComponentPropertyOptions);

        public EntityFileChangeSet GetComponentPropertyOptionThreatsMappingFileChanges() =>
            GetGlobalEntityFileChanges(DomainEntityType.ComponentPropertyOptionThreats);

        public EntityFileChangeSet GetComponentPropertyOptionThreatSecurityRequirementsMappingFileChanges() =>
            GetGlobalEntityFileChanges(DomainEntityType.ComponentPropertyOptionThreatSecurityRequirements);

        public EntityFileChangeSet GetComponentThreatMappingFileChanges() =>
            GetGlobalEntityFileChanges(DomainEntityType.ComponentThreat);

        public EntityFileChangeSet GetComponentThreatSecurityRequirementsMappingFileChanges() =>
            GetGlobalEntityFileChanges(DomainEntityType.ComponentThreatSecurityRequirements);

        public EntityFileChangeSet GetComponentSecurityRequirementsMappingFileChanges() =>
            GetGlobalEntityFileChanges(DomainEntityType.ComponentSecurityRequirements);

        public EntityFileChangeSet GetThreatSecurityRequirementsMappingFileChanges() =>
            GetGlobalEntityFileChanges(DomainEntityType.ThreatSecurityRequirements);

        #endregion

        #region Core grouping logic

        private sealed class RelativeChangeAccumulator
        {
            public List<string> Added { get; } = new();
            public List<string> Deleted { get; } = new();
            public List<string> Modified { get; } = new();
        }

        private Dictionary<(string? libraryId, DomainEntityType entityType), RelativeChangeAccumulator>
            BuildRelativeChangeMap()
        {
            var map = new Dictionary<(string? libraryId, DomainEntityType entityType), RelativeChangeAccumulator>();

            void AccumulateFromStrings(IEnumerable<string> paths, Action<RelativeChangeAccumulator, string> addAction)
            {
                foreach (var relativePath in paths ?? Enumerable.Empty<string>())
                {
                    if (string.IsNullOrWhiteSpace(relativePath))
                    {
                        continue;
                    }

                    var info = _pathClassifier.Classify(relativePath);
                    if (info.EntityType == DomainEntityType.Unknown)
                    {
                        continue;
                    }

                    var key = (info.LibraryId, info.EntityType);

                    if (!map.TryGetValue(key, out var acc))
                    {
                        acc = new RelativeChangeAccumulator();
                        map[key] = acc;
                    }

                    addAction(acc, relativePath);
                }
            }

            void AccumulateFromModified(
                IEnumerable<ThreatModeler.TF.Git.Contract.PathProcessor.ModifiedFilePathInfo> modifiedItems,
                Action<RelativeChangeAccumulator, string> addAction)
            {
                foreach (var item in modifiedItems ?? Enumerable.Empty<ThreatModeler.TF.Git.Contract.PathProcessor.ModifiedFilePathInfo>())
                {
                    if (item == null || string.IsNullOrWhiteSpace(item.RelativePath))
                    {
                        continue;
                    }

                    var relativePath = item.RelativePath;
                    var info = _pathClassifier.Classify(relativePath);
                    if (info.EntityType == DomainEntityType.Unknown)
                    {
                        continue;
                    }

                    var key = (info.LibraryId, info.EntityType);

                    if (!map.TryGetValue(key, out var acc))
                    {
                        acc = new RelativeChangeAccumulator();
                        map[key] = acc;
                    }

                    addAction(acc, relativePath);
                }
            }

            // Added / Deleted are relative strings
            AccumulateFromStrings(_diff.AddedPaths, (acc, p) => acc.Added.Add(p));
            AccumulateFromStrings(_diff.DeletedPaths, (acc, p) => acc.Deleted.Add(p));

            // Modified are string (from previous changes)
            AccumulateFromStrings(_diff.ModifiedPaths, (acc, p) => acc.Modified.Add(p));

            return map;
        }

        private IReadOnlyCollection<LibraryEntityFileChanges> GetLibraryEntityFileChanges(
            DomainEntityType entityType)
        {
            var relativeMap = BuildRelativeChangeMap();
            var result = new List<LibraryEntityFileChanges>();

            foreach (var kvp in relativeMap)
            {
                var (libraryId, type) = kvp.Key;
                if (type != entityType || string.IsNullOrWhiteSpace(libraryId))
                {
                    continue;
                }

                var acc = kvp.Value;
                var absoluteSet = new EntityFileChangeSet();

                foreach (var relative in acc.Added)
                {
                    absoluteSet.AddedFilePaths.Add(ToBaseAbsolutePath(relative));
                }

                foreach (var relative in acc.Modified)
                {
                    var basePath = ToBaseAbsolutePath(relative);
                    var targetPath = ToTargetAbsolutePath(relative);

                    absoluteSet.ModifiedFiles.Add(
                        new ModifiedFilePathInfo(relative, basePath, targetPath));
                }

                foreach (var relative in acc.Deleted)
                {
                    absoluteSet.DeletedFilePaths.Add(ToTargetAbsolutePath(relative));
                }

                result.Add(new LibraryEntityFileChanges(libraryId!, absoluteSet));
            }

            return result;
        }

        private EntityFileChangeSet GetAggregatedLibraryEntityFileChanges(
            DomainEntityType entityType)
        {
            var relativeMap = BuildRelativeChangeMap();
            var result = new EntityFileChangeSet();

            foreach (var kvp in relativeMap)
            {
                var (_, type) = kvp.Key;
                if (type != entityType)
                {
                    continue;
                }

                var acc = kvp.Value;

                foreach (var relative in acc.Added)
                {
                    result.AddedFilePaths.Add(ToBaseAbsolutePath(relative));
                }

                foreach (var relative in acc.Modified)
                {
                    var basePath = ToBaseAbsolutePath(relative);
                    var targetPath = ToTargetAbsolutePath(relative);

                    result.ModifiedFiles.Add(
                        new ModifiedFilePathInfo(relative, basePath, targetPath));
                }

                foreach (var relative in acc.Deleted)
                {
                    result.DeletedFilePaths.Add(ToTargetAbsolutePath(relative));
                }
            }

            return result;
        }

        private EntityFileChangeSet GetGlobalEntityFileChanges(
            DomainEntityType entityType)
        {
            var relativeMap = BuildRelativeChangeMap();
            var changes = new EntityFileChangeSet();

            foreach (var kvp in relativeMap)
            {
                var (libraryId, type) = kvp.Key;
                if (type != entityType || libraryId != null)
                {
                    continue;
                }

                var acc = kvp.Value;

                foreach (var relative in acc.Added)
                {
                    changes.AddedFilePaths.Add(ToBaseAbsolutePath(relative));
                }

                foreach (var relative in acc.Modified)
                {
                    var basePath = ToBaseAbsolutePath(relative);
                    var targetPath = ToTargetAbsolutePath(relative);

                    changes.ModifiedFiles.Add(
                        new ModifiedFilePathInfo(relative, basePath, targetPath));
                }

                foreach (var relative in acc.Deleted)
                {
                    changes.DeletedFilePaths.Add(ToTargetAbsolutePath(relative));
                }
            }

            return changes;
        }

        #endregion

        #region Path helpers

        private string ToBaseAbsolutePath(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(_diff.BaseRepositoryPath))
            {
                return NormalizePath(relativePath);
            }

            return Path.Combine(_diff.BaseRepositoryPath, NormalizePath(relativePath));
        }

        private string ToTargetAbsolutePath(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(_diff.TargetRepositoryPath))
            {
                return NormalizePath(relativePath);
            }

            return Path.Combine(_diff.TargetRepositoryPath, NormalizePath(relativePath));
        }

        private static string NormalizePath(string relativePath) =>
            relativePath.Replace('/', Path.DirectorySeparatorChar);

        #endregion
    }
}
