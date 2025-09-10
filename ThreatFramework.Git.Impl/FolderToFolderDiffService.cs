using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using ThreatFramework.Core.Git;
using ThreatFramework.Git.Contract;

namespace ThreatFramework.Git.Impl
{
    public class FolderToFolderDiffService : IFolderToFolderDiffService
    {
        private readonly ILogger<FolderToFolderDiffService> _log;

        public FolderToFolderDiffService(ILogger<FolderToFolderDiffService> log)
        {
            _log = log;
        }

        public async Task<DiffSummaryResponse> CompareAsync(FolderToFolderDiffRequest request, CancellationToken ct)
        {
            ValidatePaths(request);

            // Create ONE temp repo where we will create TWO commits:
            // A) baseline folder snapshot    B) target folder snapshot
            var arenaDir = Path.Combine(Path.GetTempPath(), $"arena-{Guid.NewGuid()}");
            Repository.Init(arenaDir);
            using var arenaRepo = new Repository(arenaDir);
            var sig = new Signature("DiffBot", "diff@local", DateTimeOffset.Now);

            // Commit A: snapshot files from the baseline folder
            CopyFiles(request.BaselineFolderPath, arenaDir, excludeGitFolder: true);
            Commands.Stage(arenaRepo, "*");
            var commitA = arenaRepo.Commit("baseline folder snapshot", sig, sig);

            // Clean working directory for next snapshot (keep .git)
            ResetWorkingTreeToEmpty(arenaDir);

            // Commit B: snapshot files from the target folder
            CopyFiles(request.TargetFolderPath, arenaDir, excludeGitFolder: true);
            Commands.Stage(arenaRepo, "*");
            var commitB = arenaRepo.Commit("target folder snapshot", sig, sig);

            // Diff inside the SAME repo (commitA -> commitB)
            var patch = arenaRepo.Diff.Compare<Patch>(commitA.Tree, commitB.Tree);

            var addedFiles = new List<string>();
            var removedFiles = new List<string>();
            var modifiedFiles = new List<string>();
            var renamedFiles = new List<RenamedFile>();

            foreach (var entry in patch)
            {
                switch (entry.Status)
                {
                    case ChangeKind.Added:
                        addedFiles.Add(Norm(entry.Path));
                        break;
                    case ChangeKind.Deleted:
                        removedFiles.Add(Norm(entry.Path));
                        break;
                    case ChangeKind.Modified:
                        modifiedFiles.Add(Norm(entry.Path));
                        break;
                    case ChangeKind.Renamed:
                        renamedFiles.Add(new RenamedFile(Norm(entry.OldPath), Norm(entry.Path)));
                        break;
                }
            }

            // Cleanup temp dirs
            TryDeleteDir(arenaDir);

            return await Task.FromResult(new DiffSummaryResponse(
                request.BaselineFolderPath,
                request.TargetFolderPath,
                addedFiles.Count,
                removedFiles.Count,
                modifiedFiles.Count,
                renamedFiles.Count,
                addedFiles,
                removedFiles,
                modifiedFiles,
                renamedFiles
            ));
        }

        private static string Norm(string p) => p.Replace('\\', '/');

        private static void ValidatePaths(FolderToFolderDiffRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.BaselineFolderPath) || !Directory.Exists(req.BaselineFolderPath))
                throw new DirectoryNotFoundException($"Baseline folder not found: {req.BaselineFolderPath}");
            if (string.IsNullOrWhiteSpace(req.TargetFolderPath) || !Directory.Exists(req.TargetFolderPath))
                throw new DirectoryNotFoundException($"Target folder not found: {req.TargetFolderPath}");
        }

        private static void CopyFiles(string source, string dest, bool excludeGitFolder)
        {
            foreach (var dir in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
            {
                if (excludeGitFolder && dir.EndsWith(Path.DirectorySeparatorChar + ".git", StringComparison.OrdinalIgnoreCase))
                    continue;

                var relDir = Path.GetRelativePath(source, dir);
                var targetDir = Path.Combine(dest, relDir);
                Directory.CreateDirectory(targetDir);
            }

            foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                if (excludeGitFolder && file.Contains(Path.DirectorySeparatorChar + ".git" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    continue;

                var rel = Path.GetRelativePath(source, file);
                var targetPath = Path.Combine(dest, rel);
                Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
                File.Copy(file, targetPath, true);
            }
        }

        // Remove all files and folders from working tree EXCEPT the .git folder
        private static void ResetWorkingTreeToEmpty(string repoRoot)
        {
            foreach (var dir in Directory.GetDirectories(repoRoot))
            {
                var name = Path.GetFileName(dir);
                if (string.Equals(name, ".git", StringComparison.OrdinalIgnoreCase)) continue;
                TryDeleteDir(dir);
            }
            foreach (var file in Directory.GetFiles(repoRoot))
            {
                File.Delete(file);
            }
        }

        private static void TryDeleteDir(string path)
        {
            try { if (Directory.Exists(path)) Directory.Delete(path, true); } catch { /* ignore */ }
        }
    }
}
