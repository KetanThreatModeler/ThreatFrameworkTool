using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ThreatModeler.TF.Git.Contract;
using ThreatModeler.TF.Git.Contract.Models;

namespace ThreatModeler.TF.Git.Implementation
{
    public class GitFolderDiffService : IGitFolderDiffService
    {
        private readonly ILogger<GitFolderDiffService> _logger;

        public GitFolderDiffService(ILogger<GitFolderDiffService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region Public API

        public Task<FolderDiffReport> CompareFoldersAsync(
            string repo1Path,
            string repo2Path,
            List<string> foldersToCheck,
            bool includeUncommittedChanges = false)
        {
            ValidateRepoPath(repo1Path, nameof(repo1Path));
            ValidateRepoPath(repo2Path, nameof(repo2Path));

            EnsureRepositoryReady(repo1Path, includeUncommittedChanges);
            EnsureRepositoryReady(repo2Path, includeUncommittedChanges);

            if (foldersToCheck == null || foldersToCheck.Count == 0)
            {
                throw new ArgumentException("At least one folder must be provided for comparison.", nameof(foldersToCheck));
            }

            _logger.LogInformation(
                "Starting folder comparison between repositories '{Repo1}' and '{Repo2}' for {FolderCount} folder(s). IncludeUncommittedChanges={IncludeUncommitted}.",
                repo1Path,
                repo2Path,
                foldersToCheck.Count,
                includeUncommittedChanges);

            var reports = new ConcurrentBag<FolderDiffReport>();

            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount
            };

            Parallel.ForEach(
                foldersToCheck,
                options,
                folderPath =>
                {
                    try
                    {
                        using var repo1 = new Repository(repo1Path);
                        using var repo2 = new Repository(repo2Path);

                        var tree1 = repo1.Head?.Tip?.Tree;
                        var tree2 = repo2.Head?.Tip?.Tree;

                        if (tree1 == null || tree2 == null)
                        {
                            _logger.LogWarning(
                                "Unable to obtain HEAD tree(s) for folder comparison. Repo1TreeNull={Repo1TreeNull}, Repo2TreeNull={Repo2TreeNull}",
                                tree1 == null,
                                tree2 == null);
                            return;
                        }

                        var localReport = new FolderDiffReport(repo1Path, repo2Path);

                        var folderEntry1 = FindTreeEntry(tree1, folderPath);
                        var folderEntry2 = FindTreeEntry(tree2, folderPath);

                        CompareTreesRecursive(
                            repo1,
                            folderEntry1?.Target as Tree,
                            repo2,
                            folderEntry2?.Target as Tree,
                            folderPath,
                            localReport);

                        reports.Add(localReport);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Error while comparing folder '{FolderPath}' between repositories '{Repo1}' and '{Repo2}'.",
                            folderPath,
                            repo1Path,
                            repo2Path);

                        throw;
                    }
                });

            var finalReport = new FolderDiffReport(repo1Path, repo2Path);
            foreach (var report in reports)
            {
                finalReport.Merge(report);
            }

            _logger.LogInformation(
                "Completed folder comparison between '{Repo1}' and '{Repo2}'. Added={Added}, Deleted={Deleted}, Modified={Modified}.",
                repo1Path,
                repo2Path,
                finalReport.AddedPaths.Count,
                finalReport.DeletedPaths.Count,
                finalReport.ModifiedPaths.Count);

            return Task.FromResult(finalReport);
        }

        public Task<FolderDiffReport> CompareByPrefixAsync(
            string repo1Path,
            string repo2Path,
            string folderPath,
            List<string> prefixes,
            bool includeUncommittedChanges = false)
        {
            ValidateRepoPath(repo1Path, nameof(repo1Path));
            ValidateRepoPath(repo2Path, nameof(repo2Path));

            EnsureRepositoryReady(repo1Path, includeUncommittedChanges);
            EnsureRepositoryReady(repo2Path, includeUncommittedChanges);

            if (string.IsNullOrWhiteSpace(folderPath))
            {
                throw new ArgumentException("Folder path cannot be null or empty.", nameof(folderPath));
            }

            if (prefixes == null || prefixes.Count == 0)
            {
                throw new ArgumentException("At least one prefix must be provided.", nameof(prefixes));
            }

            _logger.LogInformation(
                "Starting prefix-based comparison in folder '{FolderPath}' between '{Repo1}' and '{Repo2}' with {PrefixCount} prefix(es). IncludeUncommittedChanges={IncludeUncommitted}.",
                folderPath,
                repo1Path,
                repo2Path,
                prefixes.Count,
                includeUncommittedChanges);

            var report = new FolderDiffReport(repo1Path, repo2Path);
            var prefixSet = new HashSet<string>(prefixes);

            try
            {
                using var repo1 = new Repository(repo1Path);
                using var repo2 = new Repository(repo2Path);

                var rootTree1 = repo1.Head?.Tip?.Tree;
                var rootTree2 = repo2.Head?.Tip?.Tree;

                if (rootTree1 == null && rootTree2 == null)
                {
                    _logger.LogWarning(
                        "Unable to obtain HEAD tree(s) for prefix comparison. Both repositories have no HEAD. Repo1='{Repo1}', Repo2='{Repo2}'.",
                        repo1Path,
                        repo2Path);
                    return Task.FromResult(report);
                }

                var treeEntry1 = rootTree1 != null ? FindTreeEntry(rootTree1, folderPath) : null;
                var treeEntry2 = rootTree2 != null ? FindTreeEntry(rootTree2, folderPath) : null;

                if (treeEntry1 == null && treeEntry2 == null)
                {
                    _logger.LogWarning(
                        "Folder path '{FolderPath}' not found in either repository '{Repo1}' or '{Repo2}'.",
                        folderPath,
                        repo1Path,
                        repo2Path);

                    return Task.FromResult(report);
                }

                var tree1 = treeEntry1?.Target as Tree;
                var tree2 = treeEntry2?.Target as Tree;

                var files1 = GetRelevantFiles(tree1, folderPath, prefixSet);
                var files2 = GetRelevantFiles(tree2, folderPath, prefixSet);

                var allKeys = files1.Keys.Union(files2.Keys, StringComparer.OrdinalIgnoreCase);

                foreach (var fullPath in allKeys)
                {
                    var existsIn1 = files1.TryGetValue(fullPath, out var entry1);
                    var existsIn2 = files2.TryGetValue(fullPath, out var entry2);

                    if (existsIn1 && !existsIn2)
                    {
                        report.AddedPaths.Add(fullPath);
                    }
                    else if (!existsIn1 && existsIn2)
                    {
                        report.DeletedPaths.Add(fullPath);
                    }
                    else if (entry1 != null && entry2 != null && entry1.Target.Id != entry2.Target.Id)
                    {
                        report.ModifiedPaths.Add(fullPath);
                    }
                }

                _logger.LogInformation(
                    "Completed prefix-based comparison for folder '{FolderPath}'. Added={Added}, Deleted={Deleted}, Modified={Modified}.",
                    folderPath,
                    report.AddedPaths.Count,
                    report.DeletedPaths.Count,
                    report.ModifiedPaths.Count);

                return Task.FromResult(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error during prefix-based comparison in folder '{FolderPath}' between '{Repo1}' and '{Repo2}'.",
                    folderPath,
                    repo1Path,
                    repo2Path);

                throw;
            }
        }

        public Task<FolderDiffReport> CompareRepoWithExclusionsAsync(
             string repo1Path,
             string repo2Path,
             List<string> filesToIgnore,
             bool includeUncommittedChanges = false)
        {
            ValidateRepoPath(repo1Path, nameof(repo1Path));
            ValidateRepoPath(repo2Path, nameof(repo2Path));

            EnsureRepositoryReady(repo1Path, includeUncommittedChanges);
            EnsureRepositoryReady(repo2Path, includeUncommittedChanges);

            var ignoreSet = new HashSet<string>(
                filesToIgnore ?? Enumerable.Empty<string>(),
                StringComparer.OrdinalIgnoreCase);

            _logger.LogInformation(
                "Starting repository comparison between '{Repo1}' and '{Repo2}' with {IgnoreCount} file(s) to ignore. IncludeUncommittedChanges={IncludeUncommitted}.",
                repo1Path,
                repo2Path,
                ignoreSet.Count,
                includeUncommittedChanges);

            var report = new FolderDiffReport(repo1Path, repo2Path);

            try
            {
                using var repo1 = new Repository(repo1Path);
                using var repo2 = new Repository(repo2Path);

                var tree1 = repo1.Head?.Tip?.Tree;
                var tree2 = repo2.Head?.Tip?.Tree;

                if (tree1 == null && tree2 == null)
                {
                    _logger.LogWarning(
                        "Both repositories have no HEAD tree. Comparison aborted. Repo1Path='{Repo1}', Repo2Path='{Repo2}'.",
                        repo1Path,
                        repo2Path);
                    return Task.FromResult(report);
                }

                CompareTreesRecursive(tree1, tree2, string.Empty, report, ignoreSet);

                _logger.LogInformation(
                    "Completed repository comparison between '{Repo1}' and '{Repo2}'. Added={Added}, Deleted={Deleted}, Modified={Modified}.",
                    repo1Path,
                    repo2Path,
                    report.AddedPaths.Count,
                    report.DeletedPaths.Count,
                    report.ModifiedPaths.Count);

                return Task.FromResult(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error during repository comparison between '{Repo1}' and '{Repo2}'.",
                    repo1Path,
                    repo2Path);

                throw;
            }
        }

        #endregion

        #region Tree Comparison Helpers (Folder-based)

        private void CompareTreesRecursive(
            Repository r1,
            Tree tree1,
            Repository r2,
            Tree tree2,
            string currentPath,
            FolderDiffReport report)
        {
            // CASE 1: Folder only in repo1 => all added
            if (tree1 != null && tree2 == null)
            {
                CollectAllPaths(tree1, currentPath, report.AddedPaths);
                return;
            }

            // CASE 2: Folder only in repo2 => all deleted
            if (tree1 == null && tree2 != null)
            {
                CollectAllPaths(tree2, currentPath, report.DeletedPaths);
                return;
            }

            if (tree1 == null && tree2 == null)
            {
                return;
            }

            var dict1 = tree1.ToDictionary(x => x.Name);
            var dict2 = tree2.ToDictionary(x => x.Name);

            var allKeys = dict1.Keys.Union(dict2.Keys);

            foreach (var name in allKeys)
            {
                var itemPath = string.IsNullOrEmpty(currentPath)
                    ? name
                    : $"{currentPath}/{name}";

                var entry1 = dict1.ContainsKey(name) ? dict1[name] : null;
                var entry2 = dict2.ContainsKey(name) ? dict2[name] : null;

                if (entry1 != null && entry2 == null)
                {
                    if (entry1.TargetType == TreeEntryTargetType.Blob)
                    {
                        report.AddedPaths.Add(itemPath);
                    }
                    else
                    {
                        CollectAllPaths(entry1.Target as Tree, itemPath, report.AddedPaths);
                    }
                }
                else if (entry1 == null && entry2 != null)
                {
                    if (entry2.TargetType == TreeEntryTargetType.Blob)
                    {
                        report.DeletedPaths.Add(itemPath);
                    }
                    else
                    {
                        CollectAllPaths(entry2.Target as Tree, itemPath, report.DeletedPaths);
                    }
                }
                else if (entry1 != null && entry2 != null)
                {
                    if (entry1.TargetType == TreeEntryTargetType.Blob &&
                        entry2.TargetType == TreeEntryTargetType.Blob)
                    {
                        if (entry1.Target.Id != entry2.Target.Id)
                        {
                            report.ModifiedPaths.Add(itemPath);
                        }
                    }
                    else if (entry1.TargetType == TreeEntryTargetType.Tree &&
                             entry2.TargetType == TreeEntryTargetType.Tree)
                    {
                        CompareTreesRecursive(
                            r1,
                            entry1.Target as Tree,
                            r2,
                            entry2.Target as Tree,
                            itemPath,
                            report);
                    }
                    else
                    {
                        // Type change (file <-> folder)
                        report.ModifiedPaths.Add(itemPath);
                    }
                }
            }
        }

        #endregion

        #region Tree Comparison Helpers (Exclusion-based)

        private void CompareTreesRecursive(
            Tree tree1,
            Tree tree2,
            string currentPath,
            FolderDiffReport report,
            HashSet<string> ignoreSet)
        {
            var dict1 = tree1?.ToDictionary(x => x.Name) ?? new Dictionary<string, TreeEntry>();
            var dict2 = tree2?.ToDictionary(x => x.Name) ?? new Dictionary<string, TreeEntry>();

            var allKeys = dict1.Keys.Union(dict2.Keys);

            foreach (var name in allKeys)
            {
                if (ignoreSet.Contains(name))
                {
                    continue;
                }

                var entry1 = dict1.ContainsKey(name) ? dict1[name] : null;
                var entry2 = dict2.ContainsKey(name) ? dict2[name] : null;

                var itemPath = string.IsNullOrEmpty(currentPath)
                    ? name
                    : $"{currentPath}/{name}";

                if (entry1 != null && entry2 == null)
                {
                    if (entry1.TargetType == TreeEntryTargetType.Blob)
                    {
                        report.AddedPaths.Add(itemPath);
                    }
                    else
                    {
                        CollectAllPaths(entry1.Target as Tree, itemPath, report.AddedPaths, ignoreSet);
                    }
                }
                else if (entry1 == null && entry2 != null)
                {
                    if (entry2.TargetType == TreeEntryTargetType.Blob)
                    {
                        report.DeletedPaths.Add(itemPath);
                    }
                    else
                    {
                        CollectAllPaths(entry2.Target as Tree, itemPath, report.DeletedPaths, ignoreSet);
                    }
                }
                else if (entry1 != null && entry2 != null)
                {
                    if (entry1.TargetType == TreeEntryTargetType.Blob &&
                        entry2.TargetType == TreeEntryTargetType.Blob)
                    {
                        if (entry1.Target.Id != entry2.Target.Id)
                        {
                            report.ModifiedPaths.Add(itemPath);
                        }
                    }
                    else if (entry1.TargetType == TreeEntryTargetType.Tree &&
                             entry2.TargetType == TreeEntryTargetType.Tree)
                    {
                        CompareTreesRecursive(
                            entry1.Target as Tree,
                            entry2.Target as Tree,
                            itemPath,
                            report,
                            ignoreSet);
                    }
                }
            }
        }

        #endregion

        #region Tree Utilities

        private void CollectAllPaths(Tree tree, string currentPath, List<string> collection)
        {
            if (tree == null) return;

            foreach (var entry in tree)
            {
                var fullPath = string.IsNullOrEmpty(currentPath)
                    ? entry.Name
                    : $"{currentPath}/{entry.Name}";

                if (entry.TargetType == TreeEntryTargetType.Blob)
                {
                    collection.Add(fullPath);
                }
                else if (entry.TargetType == TreeEntryTargetType.Tree)
                {
                    CollectAllPaths(entry.Target as Tree, fullPath, collection);
                }
            }
        }

        private void CollectAllPaths(
            Tree tree,
            string currentPath,
            List<string> collection,
            HashSet<string> ignoreSet)
        {
            if (tree == null) return;

            foreach (var entry in tree)
            {
                if (ignoreSet.Contains(entry.Name)) continue;

                var fullPath = string.IsNullOrEmpty(currentPath)
                    ? entry.Name
                    : $"{currentPath}/{entry.Name}";

                if (entry.TargetType == TreeEntryTargetType.Blob)
                {
                    collection.Add(fullPath);
                }
                else if (entry.TargetType == TreeEntryTargetType.Tree)
                {
                    CollectAllPaths(entry.Target as Tree, fullPath, collection, ignoreSet);
                }
            }
        }

        /// <summary>
        /// Recursively collects files whose names start with any of the given prefixes (e.g. "25_")
        /// and returns them keyed by full path (e.g. "mappings/component-property/25_29.yaml").
        /// </summary>
        private Dictionary<string, TreeEntry> GetRelevantFiles(Tree tree, string basePath, HashSet<string> prefixSet)
        {
            var result = new Dictionary<string, TreeEntry>(StringComparer.OrdinalIgnoreCase);
            var normalizedBasePath = basePath?.TrimEnd('/') ?? string.Empty;

            CollectRelevantFiles(tree, normalizedBasePath, prefixSet, result);

            return result;
        }

        private void CollectRelevantFiles(
            Tree tree,
            string currentPath,
            HashSet<string> prefixSet,
            Dictionary<string, TreeEntry> result)
        {
            if (tree == null) return;

            foreach (var entry in tree)
            {
                var fullPath = string.IsNullOrEmpty(currentPath)
                    ? entry.Name
                    : $"{currentPath}/{entry.Name}";

                if (entry.TargetType == TreeEntryTargetType.Tree)
                {
                    // Recurse into subfolder
                    CollectRelevantFiles(entry.Target as Tree, fullPath, prefixSet, result);
                }
                else if (entry.TargetType == TreeEntryTargetType.Blob)
                {
                    if (IsPrefixMatch(entry.Name, prefixSet))
                    {
                        result[fullPath] = entry;
                    }
                }
            }
        }

        /// <summary>
        /// Matches files where the filename (without extension) contains
        /// at least one '_'-separated segment equal to any prefix in the set.
        /// Example for prefix "25":
        ///   - "25_29.yaml"       -> match
        ///   - "25_29_65.yaml"    -> match
        ///   - "30_25_65_44.yaml" -> match
        ///   - "30_26_65_25.yaml" -> match
        /// </summary>
        private bool IsFileMatch(string filename, HashSet<string> prefixSet)
        {
            var segments = GetNameSegments(filename);
            if (segments == null || prefixSet == null || prefixSet.Count == 0)
                return false;

            var normalizedPrefixes = EnsureCaseInsensitive(prefixSet);

            foreach (var segment in segments)
            {
                if (normalizedPrefixes.Contains(segment))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Matches files where the filename starts with prefix + "_".
        /// Example prefix: "25" → matches "25_29.yaml", "25_12.txt"
        /// </summary>
        /// <summary>
        /// Checks whether the filename starts with ANY prefix from the set,
        /// followed by an underscore. 
        /// Example prefix "25" → matches "25_29.yaml"
        /// </summary>
        private bool IsPrefixMatch(string filename, HashSet<string> prefixes)
        {
            if (string.IsNullOrWhiteSpace(filename) || prefixes == null || prefixes.Count == 0)
                return false;

            var name = Path.GetFileNameWithoutExtension(filename);
            if (string.IsNullOrWhiteSpace(name))
                return false;

            foreach (var prefix in prefixes)
            {
                if (string.IsNullOrWhiteSpace(prefix))
                    continue;

                if (name.StartsWith(prefix + "_", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }


        #region Helpers

        /// <summary>
        /// Splits filename (no extension) into underscore-separated segments.
        /// Returns null if invalid.
        /// </summary>
        private static string[]? GetNameSegments(string filename)
        {
            if (string.IsNullOrWhiteSpace(filename))
                return null;

            var name = Path.GetFileNameWithoutExtension(filename);
            return string.IsNullOrWhiteSpace(name)
                ? null
                : name.Split('_', StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Ensures prefix set is using a case-insensitive comparer.
        /// </summary>
        private static HashSet<string> EnsureCaseInsensitive(HashSet<string> set)
        {
            return set.Comparer == StringComparer.OrdinalIgnoreCase
                ? set
                : new HashSet<string>(set, StringComparer.OrdinalIgnoreCase);
        }

        #endregion



        private TreeEntry? FindTreeEntry(Tree root, string path)
        {
            if (root == null || string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            try
            {
                return root[path];
            }
            catch (NotFoundException)
            {
                _logger.LogWarning("Folder path '{Path}' not found in the Git tree.", path);
                return null;
            }
        }

        #endregion

        #region Repo / Path Helpers

        private void ValidateRepoPath(string path, string paramName)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Repository path cannot be null or empty.", paramName);
            }

            if (!Directory.Exists(path))
            {
                throw new DirectoryNotFoundException($"Repository directory '{path}' does not exist.");
            }
        }

        /// <summary>
        /// Ensures that the given path is a valid Git repository, has an initial commit,
        /// and optionally creates a snapshot commit including uncommitted changes.
        /// </summary>
        private void EnsureRepositoryReady(string repoPath, bool includeUncommittedChanges)
        {
            try
            {
                if (!Repository.IsValid(repoPath))
                {
                    _logger.LogInformation(
                        "No valid Git repository found at '{RepoPath}'. Initializing a new repository.",
                        repoPath);

                    Repository.Init(repoPath);
                }

                using var repo = new Repository(repoPath);

                EnsureInitialCommit(repo, repoPath);

                if (includeUncommittedChanges)
                {
                    EnsureSnapshotCommit(repo, repoPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to prepare Git repository at '{RepoPath}'.",
                    repoPath);

                throw;
            }
        }

        /// <summary>
        /// Ensures there is at least one commit in the repository. If there is none,
        /// creates an initial commit from the current working directory.
        /// </summary>
        private void EnsureInitialCommit(Repository repo, string repoPath)
        {
            if (repo.Head?.Tip != null)
            {
                return;
            }

            var workingDirectory = repo.Info.WorkingDirectory;

            if (!Directory.EnumerateFileSystemEntries(workingDirectory).Any())
            {
                _logger.LogWarning(
                    "Repository at '{RepoPath}' has no commits and the working directory is empty. " +
                    "Diff operations will treat it as an empty repository.",
                    repoPath);
                return;
            }

            _logger.LogInformation(
                "Repository at '{RepoPath}' has no commits. Creating initial auto-generated commit for diff baseline.",
                repoPath);

            Commands.Stage(repo, "*");

            var now = DateTimeOffset.Now;
            var signature = new Signature(
                "ThreatFrameworkTool AutoInit",
                "autoinit@threatmodeler.com",
                now);

            repo.Commit(
                "Initial auto-generated commit for diff baseline",
                signature,
                signature);

            _logger.LogInformation(
                "Initial commit successfully created for repository at '{RepoPath}'.",
                repoPath);
        }

        /// <summary>
        /// If the repository has uncommitted changes, creates a snapshot commit capturing the
        /// current working directory state so that diffs reflect uncommitted changes as well.
        /// </summary>
        private void EnsureSnapshotCommit(Repository repo, string repoPath)
        {
            var status = repo.RetrieveStatus(new StatusOptions
            {
                IncludeUntracked = true,
                Show = StatusShowOption.IndexAndWorkDir
            });

            if (!status.IsDirty)
            {
                _logger.LogDebug(
                    "Repository at '{RepoPath}' has no uncommitted changes. No snapshot commit required.",
                    repoPath);
                return;
            }

            _logger.LogInformation(
                "Repository at '{RepoPath}' has uncommitted changes. Creating snapshot commit for diff.",
                repoPath);

            Commands.Stage(repo, "*");

            var now = DateTimeOffset.Now;
            var signature = new Signature(
                "ThreatFrameworkTool Snapshot",
                "snapshot@threatmodeler.com",
                now);

            repo.Commit(
                "Snapshot commit for diff (include uncommitted changes)",
                signature,
                signature);

            _logger.LogInformation(
                "Snapshot commit created for repository at '{RepoPath}' to include uncommitted changes in diff.",
                repoPath);
        }

        #endregion
    }
}
