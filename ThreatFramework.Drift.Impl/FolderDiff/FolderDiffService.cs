using LibGit2Sharp;
using ThreatFramework.Drift.Contract.FolderDiff;

namespace ThreatFramework.Drift.Impl.FolderDiff
{
    public class FolderDiffService : IFolderDiffService
    {
        public Task<FolderComparisionResult> Compare(string goldenFolderPath, string clientFolderPath)
        {
            if (!Directory.Exists(goldenFolderPath))
                throw new DirectoryNotFoundException(goldenFolderPath);
            if (!Directory.Exists(clientFolderPath))
                throw new DirectoryNotFoundException(clientFolderPath);

            var tempRoot = Path.Combine(Path.GetTempPath(), "folder-diff-" + Guid.NewGuid());
            
            // Delete temp directory if it already exists
            if (Directory.Exists(tempRoot))
            {
                ForceDeleteDirectory(tempRoot);
            }
            
            Directory.CreateDirectory(tempRoot);

            try
            {
                Repository.Init(tempRoot, isBare: false);
                
                FolderComparisionResult result;
                using (var repo = new Repository(tempRoot))
                {
                    // Client as baseline (tree1), Golden as target (tree2)
                    // This way files in client but not in golden will show as "deleted"
                    var tree1 = BuildTree(repo, clientFolderPath);
                    var tree2 = BuildTree(repo, goldenFolderPath);

                    // Disable rename detection by setting similarity to None
                    var compareOptions = new CompareOptions
                    {
                        Similarity = SimilarityOptions.None
                    };

                    var changes = repo.Diff.Compare<TreeChanges>(tree1, tree2, compareOptions: compareOptions);

                    var added = new List<string>();
                    var removed = new List<string>();
                    var modified = new List<string>();

                    foreach (var c in changes)
                    {
                        var rel = NormalizeRel(c.Path);

                        switch (c.Status)
                        {
                            case ChangeKind.Added:
                                added.Add(rel);
                                break;

                            case ChangeKind.Deleted:
                                removed.Add(rel);
                                break;

                            case ChangeKind.Modified:
                                modified.Add(rel);
                                break;

                            // With rename detection disabled, these cases should not occur
                            case ChangeKind.Renamed:
                            case ChangeKind.Copied:
                                // Fallback - treat as modified if they somehow still occur
                                modified.Add(rel);
                                break;

                                // Other kinds aren't expected in tree-to-tree diffs
                        }
                    }

                    // BaseLineFolderPath = client (baseline), TargetFolderPath = golden (target)
                    result = new FolderComparisionResult(clientFolderPath, goldenFolderPath)
                    {
                        AddedFiles = added.OrderBy(x => x).ToList(),
                        RemovedFiles = removed.OrderBy(x => x).ToList(),
                        ModifiedFiles = modified.OrderBy(x => x).ToList()
                    };
                } // Repository is properly disposed here

                return Task.FromResult(result);
            }
            finally
            {
                ForceDeleteDirectory(tempRoot);
            }
        }

        private static string NormalizeRel(string rel) => rel.Replace('\\', '/');

        // Build a Tree from a directory without staging/committing anything.
        private static Tree BuildTree(Repository repo, string root)
        {
            var def = new TreeDefinition();
            var processedPaths = new HashSet<string>();

            foreach (var file in EnumerateFilesSkippingMapping(root))
            {
                var rel = Path.GetRelativePath(root, file);
                
                if (IsUnderMapping(rel))
                    continue;

                rel = NormalizeRel(rel);
                
                // DEBUG: Check for duplicates
                if (processedPaths.Contains(rel))
                {
                    Console.WriteLine($"WARNING: Duplicate path detected: {rel}");
                    continue;
                }
                processedPaths.Add(rel);

                // Verify file actually exists and is readable
                if (!File.Exists(file))
                {
                    Console.WriteLine($"WARNING: File not found during tree building: {file}");
                    continue;
                }

                try
                {
                    var blob = repo.ObjectDatabase.CreateBlob(file);
                    def.Add(rel, blob, Mode.NonExecutableFile);
                    Console.WriteLine($"Added to tree: {rel}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR adding {rel}: {ex.Message}");
                }
            }

            return repo.ObjectDatabase.CreateTree(def);
        }

        private static IEnumerable<string> EnumerateFilesSkippingMapping(string root)
        {
            var stack = new Stack<string>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                var dir = stack.Pop();

                // If this directory itself is Mapping/Mappings, prune the entire subtree
                if (IsMappingFolder(dir))
                    continue;

                foreach (var sub in Directory.EnumerateDirectories(dir))
                {
                    if (IsMappingFolder(sub))
                        continue; // prune early
                    stack.Push(sub);
                }

                foreach (var file in Directory.EnumerateFiles(dir))
                    yield return file;
            }
        }

        private static bool IsMappingFolder(string pathOrDir)
        {
            var name = new DirectoryInfo(pathOrDir).Name;
            return name.Equals("Mapping", StringComparison.OrdinalIgnoreCase)
                || name.Equals("Mappings", StringComparison.OrdinalIgnoreCase)
                || name.Equals("mapping", StringComparison.OrdinalIgnoreCase)
                || name.Equals("mappings", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsUnderMapping(string relativePath)
        {
            var parts = relativePath.Replace('\\', '/').Split('/');
            foreach (var p in parts)
            {
                if (p.Equals("Mapping", StringComparison.OrdinalIgnoreCase) ||
                    p.Equals("Mappings", StringComparison.OrdinalIgnoreCase) ||
                    p.Equals("mapping", StringComparison.OrdinalIgnoreCase) ||
                    p.Equals("mappings", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static void ForceDeleteDirectory(string path)
        {
            if (!Directory.Exists(path))
                return;

            const int maxRetries = 5;
            const int baseDelayMs = 100;

            Exception lastException = null;

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    // Remove read-only attributes from all files and directories
                    RemoveReadOnlyAttributes(path);
                    
                    Directory.Delete(path, recursive: true);
                    return; // Success
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    lastException = ex;
                    if (attempt < maxRetries - 1)
                    {
                        // Wait with exponential backoff
                        Thread.Sleep(baseDelayMs * (int)Math.Pow(2, attempt));
                    }
                }
            }
            
            // Final attempt with individual file deletion
            try
            {
                DeleteDirectoryContents(path);
                Directory.Delete(path);
            }
            catch (Exception ex)
            {
                throw new IOException($"Failed to delete temporary directory '{path}' after {maxRetries} attempts. Last error: {lastException?.Message}", ex);
            }
        }

        private static void RemoveReadOnlyAttributes(string path)
        {
            try
            {
                var directoryInfo = new DirectoryInfo(path);
                
                // Remove read-only from directory itself
                if (directoryInfo.Attributes.HasFlag(FileAttributes.ReadOnly))
                {
                    directoryInfo.Attributes &= ~FileAttributes.ReadOnly;
                }

                // Remove read-only from all files
                foreach (var file in directoryInfo.EnumerateFiles("*", SearchOption.AllDirectories))
                {
                    if (file.Attributes.HasFlag(FileAttributes.ReadOnly))
                    {
                        file.Attributes &= ~FileAttributes.ReadOnly;
                    }
                }

                // Remove read-only from all subdirectories
                foreach (var dir in directoryInfo.EnumerateDirectories("*", SearchOption.AllDirectories))
                {
                    if (dir.Attributes.HasFlag(FileAttributes.ReadOnly))
                    {
                        dir.Attributes &= ~FileAttributes.ReadOnly;
                    }
                }
            }
            catch
            {
                // Continue if attribute removal fails
            }
        }

        private static void DeleteDirectoryContents(string path)
        {
            // Delete all files first
            foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
            {
                try
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Delete(file);
                }
                catch
                {
                    // Continue with other files
                }
            }

            // Delete directories in reverse order (deepest first)
            var directories = Directory.GetDirectories(path, "*", SearchOption.AllDirectories)
                                     .OrderByDescending(d => d.Length);
            
            foreach (var dir in directories)
            {
                try
                {
                    Directory.Delete(dir);
                }
                catch
                {
                    // Continue with other directories
                }
            }
        }
    }
}