using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatModeler.TF.Git.Contract;
using ThreatModeler.TF.Git.Contract.Models;

namespace ThreatModeler.TF.Git.Implementation
{
    public class GitFolderDiffService : IGitFolderDiffService
    {
        public Task<FolderDiffReport> CompareFoldersAsync(
            string repo1Path,
            string repo2Path,
            List<string> foldersToCheck)
        {
            var finalReport = new FolderDiffReport();

            // PARALLEL OPTIMIZATION: 
            // We process each requested root folder (e.g., "01/components", "06/threats") in parallel.
            // Note: LibGit2Sharp Repository objects are NOT thread-safe. 
            // We create a lightweight instance per thread.

            Parallel.ForEach(foldersToCheck, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, folderPath =>
            {
                var localReport = new FolderDiffReport();

                using var repo1 = new Repository(repo1Path);
                using var repo2 = new Repository(repo2Path);

                // Get the Tree (Directory State) for the HEAD commit of both repos
                // This is an O(1) lookup in the Git DB
                var tree1 = repo1.Head.Tip.Tree;
                var tree2 = repo2.Head.Tip.Tree;

                // Navigate down to the specific folder requested
                var folderEntry1 = FindTreeEntry(tree1, folderPath);
                var folderEntry2 = FindTreeEntry(tree2, folderPath);

                CompareTreesRecursive(
                    repo1, folderEntry1?.Target as Tree,
                    repo2, folderEntry2?.Target as Tree,
                    folderPath,
                    localReport
                );

                finalReport.Merge(localReport);
            });

            return Task.FromResult(finalReport);
        }

        private void CompareTreesRecursive(
            Repository r1, Tree tree1,
            Repository r2, Tree tree2,
            string currentPath,
            FolderDiffReport report)
        {
            // CASE 1: Folder missing in Repo 2 (New State has it, Old doesn't)
            // -> Mark EVERYTHING inside tree1 as ADDED
            if (tree1 != null && tree2 == null)
            {
                CollectAllPaths(tree1, currentPath, report.AddedPaths);
                return;
            }

            // CASE 2: Folder missing in Repo 1 (New State lost it, Old had it)
            // -> Mark EVERYTHING inside tree2 as DELETED
            if (tree1 == null && tree2 != null)
            {
                CollectAllPaths(tree2, currentPath, report.DeletedPaths);
                return;
            }

            // CASE 3: Both Missing (Shouldn't happen given logic, but safety check)
            if (tree1 == null && tree2 == null) return;

            // CASE 4: Both Exist - We must compare contents
            // We iterate over the UNION of items in both trees.

            // To optimize, convert to Dictionary for O(1) lookups
            var dict1 = tree1.ToDictionary(x => x.Name);
            var dict2 = tree2.ToDictionary(x => x.Name);

            var allKeys = dict1.Keys.Union(dict2.Keys);

            foreach (var name in allKeys)
            {
                var itemPath = $"{currentPath}/{name}";

                var entry1 = dict1.ContainsKey(name) ? dict1[name] : null;
                var entry2 = dict2.ContainsKey(name) ? dict2[name] : null;

                // Sub-Case A: Added
                if (entry1 != null && entry2 == null)
                {
                    if (entry1.TargetType == TreeEntryTargetType.Blob) report.AddedPaths.Add(itemPath);
                    else CollectAllPaths(entry1.Target as Tree, itemPath, report.AddedPaths);
                }
                // Sub-Case B: Deleted
                else if (entry1 == null && entry2 != null)
                {
                    if (entry2.TargetType == TreeEntryTargetType.Blob) report.DeletedPaths.Add(itemPath);
                    else CollectAllPaths(entry2.Target as Tree, itemPath, report.DeletedPaths);
                }
                // Sub-Case C: Present in both
                else
                {
                    // If it's a File (Blob), compare Hashes
                    if (entry1.TargetType == TreeEntryTargetType.Blob && entry2.TargetType == TreeEntryTargetType.Blob)
                    {
                        if (entry1.Target.Id != entry2.Target.Id)
                        {
                            report.ModifiedPaths.Add(itemPath);
                        }
                    }
                    // If it's a Folder (Tree), Recurse
                    else if (entry1.TargetType == TreeEntryTargetType.Tree && entry2.TargetType == TreeEntryTargetType.Tree)
                    {
                        CompareTreesRecursive(r1, entry1.Target as Tree, r2, entry2.Target as Tree, itemPath, report);
                    }
                    // Type Change (File became Folder or vice versa) -> Treat as Mod or Delete/Add
                    else
                    {
                        report.ModifiedPaths.Add(itemPath);
                    }
                }
            }
        }

        /// <summary>
        /// Fast helper to dump all file paths in a tree (used when whole folder is added/deleted).
        /// </summary>
        private void CollectAllPaths(Tree tree, string currentPath, List<string> collection)
        {
            if (tree == null) return;

            foreach (var entry in tree)
            {
                var fullPath = $"{currentPath}/{entry.Name}";
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

        /// <summary>
        /// Navigates the Git Tree using the path string (e.g. "01/components")
        /// </summary>
        private TreeEntry? FindTreeEntry(Tree root, string path)
        {
            // LibGit2Sharp lets you access paths directly via indexer!
            // tree["01/components"] works.
            return root[path];
        }

        public Task<FolderDiffReport> CompareByPrefixAsync(
            string repo1Path,
            string repo2Path,
            string folderPath,
            List<string> prefixes)
        {
            var report = new FolderDiffReport();

            // Optimization: Use HashSet for O(1) lookup during file filtering
            var prefixSet = new HashSet<string>(prefixes);

            using var repo1 = new Repository(repo1Path);
            using var repo2 = new Repository(repo2Path);

            // 1. Get the Tree for the specific folder (e.g., "mappings/component-property")
            // This is Instant (O(1))
            var treeEntry1 = repo1.Head.Tip.Tree[folderPath];
            var treeEntry2 = repo2.Head.Tip.Tree[folderPath];

            // 2. Extract RELEVANT files into a Dictionary (Filename -> GitEntry)
            // We pass the prefixSet to filter immediately while reading
            var files1 = GetRelevantFiles(treeEntry1?.Target as Tree, prefixSet);
            var files2 = GetRelevantFiles(treeEntry2?.Target as Tree, prefixSet);

            // 3. Compare the Dictionaries
            // Union of all filenames we care about
            var allKeys = files1.Keys.Union(files2.Keys);

            foreach (var filename in allKeys)
            {
                var existsIn1 = files1.TryGetValue(filename, out var entry1);
                var existsIn2 = files2.TryGetValue(filename, out var entry2);

                var fullPath = $"{folderPath}/{filename}";

                // CASE: ADDED (Present in 1, Missing in 2)
                if (existsIn1 && !existsIn2)
                {
                    report.AddedPaths.Add(fullPath);
                }
                // CASE: DELETED (Missing in 1, Present in 2)
                else if (!existsIn1 && existsIn2)
                {
                    report.DeletedPaths.Add(fullPath);
                }
                // CASE: MODIFIED (Present in Both, SHA Differs)
                else
                {
                    // Fast comparison using Git OID (SHA hash)
                    if (entry1.Target.Id != entry2.Target.Id)
                    {
                        report.ModifiedPaths.Add(fullPath);
                    }
                }
            }

            return Task.FromResult(report);
        }

        /// <summary>
        /// Scans a Git Tree and returns only files matching the Prefix Set.
        /// </summary>
        private Dictionary<string, TreeEntry> GetRelevantFiles(Tree tree, HashSet<string> prefixSet)
        {
            var result = new Dictionary<string, TreeEntry>();

            if (tree == null) return result;

            foreach (var entry in tree)
            {
                // We only care about Blobs (Files), not sub-folders
                if (entry.TargetType != TreeEntryTargetType.Blob) continue;

                // Optimization: Efficiently check if filename starts with any prefix
                // Format is expected to be "{prefix}_..."
                if (IsFileMatch(entry.Name, prefixSet))
                {
                    result[entry.Name] = entry;
                }
            }

            return result;
        }

        /// <summary>
        /// Checks if the filename starts with a valid ID followed by an underscore.
        /// Example: "11_12.yaml" matches prefix "11".
        /// </summary>
        private bool IsFileMatch(string filename, HashSet<string> prefixSet)
        {
            // Performance: Find the first underscore to isolate the ID part
            // "11_12.yaml" -> index of '_' is 2. Substring(0, 2) is "11".

            var underscoreIndex = filename.IndexOf('_');
            if (underscoreIndex <= 0) return false;

            // This creates a string allocation, but it's unavoidable unless we use Span<char>
            // In .NET Core/6+, using ReadOnlySpan<char> would be zero-allocation.
            var filePrefix = filename.Substring(0, underscoreIndex);

            return prefixSet.Contains(filePrefix);
        }

        public Task<FolderDiffReport> CompareRepoWithExclusionsAsync(
             string repo1Path,
             string repo2Path,
             List<string> filesToIgnore)
        {
            var report = new FolderDiffReport();

            // Optimization: HashSet for O(1) lookup of exact filenames
            var ignoreSet = new HashSet<string>(filesToIgnore, StringComparer.OrdinalIgnoreCase);

            using var repo1 = new Repository(repo1Path);
            using var repo2 = new Repository(repo2Path);

            var tree1 = repo1.Head.Tip.Tree;
            var tree2 = repo2.Head.Tip.Tree;

            // Start recursive comparison from Root ("")
            CompareTreesRecursive(tree1, tree2, "", report, ignoreSet);

            return Task.FromResult(report);
        }

        private void CompareTreesRecursive(
            Tree tree1,
            Tree tree2,
            string currentPath,
            FolderDiffReport report,
            HashSet<string> ignoreSet)
        {
            // 1. Get Union of all entries in this folder level
            //    Handle null trees (e.g. if folder doesn't exist in one repo)
            var dict1 = tree1?.ToDictionary(x => x.Name) ?? new Dictionary<string, TreeEntry>();
            var dict2 = tree2?.ToDictionary(x => x.Name) ?? new Dictionary<string, TreeEntry>();

            var allKeys = dict1.Keys.Union(dict2.Keys);

            foreach (var name in allKeys)
            {
                // --- EXCLUSION CHECK (High Performance Filter) ---
                if (ignoreSet.Contains(name)) continue;
                // You can add more complex logic here, e.g., name.EndsWith(".md")

                var entry1 = dict1.ContainsKey(name) ? dict1[name] : null;
                var entry2 = dict2.ContainsKey(name) ? dict2[name] : null;

                // Build relative path (clean handling of root)
                var itemPath = string.IsNullOrEmpty(currentPath) ? name : $"{currentPath}/{name}";

                // CASE: ADDED (Present in 1, Missing in 2)
                if (entry1 != null && entry2 == null)
                {
                    if (entry1.TargetType == TreeEntryTargetType.Blob)
                    {
                        report.AddedPaths.Add(itemPath);
                    }
                    else
                    {
                        // Bulk add entire folder content
                        CollectAllPaths(entry1.Target as Tree, itemPath, report.AddedPaths, ignoreSet);
                    }
                }
                // CASE: DELETED (Missing in 1, Present in 2)
                else if (entry1 == null && entry2 != null)
                {
                    if (entry2.TargetType == TreeEntryTargetType.Blob)
                    {
                        report.DeletedPaths.Add(itemPath);
                    }
                    else
                    {
                        // Bulk delete entire folder content
                        CollectAllPaths(entry2.Target as Tree, itemPath, report.DeletedPaths, ignoreSet);
                    }
                }
                // CASE: BOTH EXIST
                else
                {
                    // If Files: Check Hash
                    if (entry1.TargetType == TreeEntryTargetType.Blob && entry2.TargetType == TreeEntryTargetType.Blob)
                    {
                        if (entry1.Target.Id != entry2.Target.Id)
                        {
                            report.ModifiedPaths.Add(itemPath);
                        }
                    }
                    // If Folders: Recurse
                    else if (entry1.TargetType == TreeEntryTargetType.Tree && entry2.TargetType == TreeEntryTargetType.Tree)
                    {
                        CompareTreesRecursive(entry1.Target as Tree, entry2.Target as Tree, itemPath, report, ignoreSet);
                    }
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

                var fullPath = $"{currentPath}/{entry.Name}";

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

}
}
