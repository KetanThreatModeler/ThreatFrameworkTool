using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public EntityFileChangeSet GetLibraryFileChanges()
        {
            return GetAggregatedLibraryEntityFileChanges(DomainEntityType.Library);
        }

        public IReadOnlyCollection<LibraryEntityFileChanges> GetLibraryFileChangesByLibrary()
        {
            return GetLibraryEntityFileChanges(DomainEntityType.Library);
        }

        #endregion

        #region Public API – Library-specific entities (aggregated)

        public EntityFileChangeSet GetComponentFileChanges()
        {
            return GetAggregatedLibraryEntityFileChanges(DomainEntityType.Components);
        }

        public EntityFileChangeSet GetSecurityRequirementFileChanges()
        {
            return GetAggregatedLibraryEntityFileChanges(DomainEntityType.SecurityRequirements);
        }

        public EntityFileChangeSet GetTestCaseFileChanges()
        {
            return GetAggregatedLibraryEntityFileChanges(DomainEntityType.TestCases);
        }

        public EntityFileChangeSet GetThreatFileChanges()
        {
            return GetAggregatedLibraryEntityFileChanges(DomainEntityType.Threats);
        }

        public EntityFileChangeSet GetPropertyFileChanges()
        {
            return GetAggregatedLibraryEntityFileChanges(DomainEntityType.Properties);
        }

        #endregion

        #region Public API – Library-specific entities (library-wise)

        public IReadOnlyCollection<LibraryEntityFileChanges> GetComponentFileChangesByLibrary()
        {
            return GetLibraryEntityFileChanges(DomainEntityType.Components);
        }

        public IReadOnlyCollection<LibraryEntityFileChanges> GetSecurityRequirementFileChangesByLibrary()
        {
            return GetLibraryEntityFileChanges(DomainEntityType.SecurityRequirements);
        }

        public IReadOnlyCollection<LibraryEntityFileChanges> GetTestCaseFileChangesByLibrary()
        {
            return GetLibraryEntityFileChanges(DomainEntityType.TestCases);
        }

        public IReadOnlyCollection<LibraryEntityFileChanges> GetThreatFileChangesByLibrary()
        {
            return GetLibraryEntityFileChanges(DomainEntityType.Threats);
        }

        public IReadOnlyCollection<LibraryEntityFileChanges> GetPropertyFileChangesByLibrary()
        {
            return GetLibraryEntityFileChanges(DomainEntityType.Properties);
        }

        #endregion

        #region Public API – Global entities

        public EntityFileChangeSet GetComponentTypeFileChanges()
        {
            return GetGlobalEntityFileChanges(DomainEntityType.ComponentType);
        }

        public EntityFileChangeSet GetPropertyTypeFileChanges()
        {
            return GetGlobalEntityFileChanges(DomainEntityType.PropertyType);
        }

        public EntityFileChangeSet GetPropertyOptionsFileChanges()
        {
            return GetGlobalEntityFileChanges(DomainEntityType.PropertyOptions);
        }

        #endregion

        #region Public API – Mapping entities

        public EntityFileChangeSet GetComponentPropertyMappingFileChanges()
        {
            return GetGlobalEntityFileChanges(DomainEntityType.ComponentProperty);
        }

        public EntityFileChangeSet GetComponentPropertyOptionsMappingFileChanges()
        {
            return GetGlobalEntityFileChanges(DomainEntityType.ComponentPropertyOptions);
        }

        public EntityFileChangeSet GetComponentPropertyOptionThreatsMappingFileChanges()
        {
            return GetGlobalEntityFileChanges(DomainEntityType.ComponentPropertyOptionThreats);
        }

        public EntityFileChangeSet GetComponentPropertyOptionThreatSecurityRequirementsMappingFileChanges()
        {
            return GetGlobalEntityFileChanges(DomainEntityType.ComponentPropertyOptionThreatSecurityRequirements);
        }

        public EntityFileChangeSet GetComponentThreatMappingFileChanges()
        {
            return GetGlobalEntityFileChanges(DomainEntityType.ComponentThreat);
        }

        public EntityFileChangeSet GetComponentThreatSecurityRequirementsMappingFileChanges()
        {
            return GetGlobalEntityFileChanges(DomainEntityType.ComponentThreatSecurityRequirements);
        }

        public EntityFileChangeSet GetComponentSecurityRequirementsMappingFileChanges()
        {
            return GetGlobalEntityFileChanges(DomainEntityType.ComponentSecurityRequirements);
        }

        public EntityFileChangeSet GetThreatSecurityRequirementsMappingFileChanges()
        {
            return GetGlobalEntityFileChanges(DomainEntityType.ThreatSecurityRequirements);
        }

        #endregion

        #region Core grouping logic

        private sealed class RelativeChangeAccumulator
        {
            public List<string> Added { get; } = new List<string>();
            public List<string> Deleted { get; } = new List<string>();
            public List<string> Modified { get; } = new List<string>();
        }

        private enum ChangeType
        {
            Added,
            Deleted,
            Modified
        }

        private Dictionary<(string? libraryId, DomainEntityType entityType), RelativeChangeAccumulator>
            BuildRelativeChangeMap()
        {
            var map = new Dictionary<(string? libraryId, DomainEntityType entityType), RelativeChangeAccumulator>();

            // Added / Deleted / Modified are all relative strings
            AddStringPathsToMap(_diff.AddedPaths, map, ChangeType.Added);
            AddStringPathsToMap(_diff.DeletedPaths, map, ChangeType.Deleted);
            AddStringPathsToMap(_diff.ModifiedPaths, map, ChangeType.Modified);

            return map;
        }

        private void AddStringPathsToMap(
            IEnumerable<string> paths,
            Dictionary<(string? libraryId, DomainEntityType entityType), RelativeChangeAccumulator> map,
            ChangeType changeType)
        {
            var safePaths = paths ?? Enumerable.Empty<string>();

            foreach (var relativePath in safePaths)
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
                if (!map.TryGetValue(key, out var accumulator))
                {
                    accumulator = new RelativeChangeAccumulator();
                    map[key] = accumulator;
                }

                switch (changeType)
                {
                    case ChangeType.Added:
                        accumulator.Added.Add(relativePath);
                        break;
                    case ChangeType.Deleted:
                        accumulator.Deleted.Add(relativePath);
                        break;
                    case ChangeType.Modified:
                        accumulator.Modified.Add(relativePath);
                        break;
                }
            }
        }

        private IReadOnlyCollection<LibraryEntityFileChanges> GetLibraryEntityFileChanges(
            DomainEntityType entityType)
        {
            var relativeMap = BuildRelativeChangeMap();
            var result = new List<LibraryEntityFileChanges>();

            foreach (var kvp in relativeMap)
            {
                var key = kvp.Key;
                var libraryId = key.libraryId;
                var type = key.entityType;

                if (type != entityType || string.IsNullOrWhiteSpace(libraryId))
                {
                    continue;
                }

                var accumulator = kvp.Value;
                var absoluteSet = new EntityFileChangeSet();

                // Added
                foreach (var relative in accumulator.Added)
                {
                    absoluteSet.AddedFilePaths.Add(ToBaseAbsolutePath(relative));
                }

                // Modified
                foreach (var relative in accumulator.Modified)
                {
                    var basePath = ToBaseAbsolutePath(relative);
                    var targetPath = ToTargetAbsolutePath(relative);

                    absoluteSet.ModifiedFiles.Add(
                        new ModifiedFilePathInfo(relative, basePath, targetPath));
                }

                // Deleted
                foreach (var relative in accumulator.Deleted)
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
                var key = kvp.Key;
                var type = key.entityType;

                if (type != entityType)
                {
                    continue;
                }

                var accumulator = kvp.Value;

                // Added
                foreach (var relative in accumulator.Added)
                {
                    result.AddedFilePaths.Add(ToBaseAbsolutePath(relative));
                }

                // Modified
                foreach (var relative in accumulator.Modified)
                {
                    var basePath = ToBaseAbsolutePath(relative);
                    var targetPath = ToTargetAbsolutePath(relative);

                    result.ModifiedFiles.Add(
                        new ModifiedFilePathInfo(relative, basePath, targetPath));
                }

                // Deleted
                foreach (var relative in accumulator.Deleted)
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
                var key = kvp.Key;
                var libraryId = key.libraryId;
                var type = key.entityType;

                // Global entities have no library id (null)
                if (type != entityType || libraryId != null)
                {
                    continue;
                }

                var accumulator = kvp.Value;

                // Added
                foreach (var relative in accumulator.Added)
                {
                    changes.AddedFilePaths.Add(ToBaseAbsolutePath(relative));
                }

                // Modified
                foreach (var relative in accumulator.Modified)
                {
                    var basePath = ToBaseAbsolutePath(relative);
                    var targetPath = ToTargetAbsolutePath(relative);

                    changes.ModifiedFiles.Add(
                        new ModifiedFilePathInfo(relative, basePath, targetPath));
                }

                // Deleted
                foreach (var relative in accumulator.Deleted)
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

        private static string NormalizePath(string relativePath)
        {
            return relativePath.Replace('/', Path.DirectorySeparatorChar);
        }

        #endregion
    }
}
