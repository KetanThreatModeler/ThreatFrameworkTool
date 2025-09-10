using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Core.Git;
using ThreatFramework.Git.Contract;

namespace ThreatFramework.Git.Impl
{
    public class RemoteVsFolderDiffService : IDiffSummaryService
    {
        private readonly ILogger<RemoteVsFolderDiffService> _log;

        public RemoteVsFolderDiffService(ILogger<RemoteVsFolderDiffService> log)
        {
            _log = log;
        }

        public async Task<DiffSummaryResponse> CompareAsync(DiffSummaryRequest request, CancellationToken ct)
        {
            ValidatePaths(request);

            // 1) Clone the remote baseline repo to a temp working dir (to read files)
            var baselineCloneDir = Path.Combine(Path.GetTempPath(), $"baseline-{Guid.NewGuid()}");
            Repository.Clone(request.RemoteRepoUrl, baselineCloneDir);
            using var baselineRepo = new Repository(baselineCloneDir);

            // (Optional) choose a specific branch/commit here if you want:
            // var baselineCommit = baselineRepo.Lookup<Commit>("origin/main"); // or a SHA/branch/tag
            var baselineCommit = baselineRepo.Head.Tip;

            // 2) Create ONE temp repo where we will create TWO commits:
            //    A) baseline snapshot    B) target snapshot
            var arenaDir = Path.Combine(Path.GetTempPath(), $"arena-{Guid.NewGuid()}");
            Repository.Init(arenaDir);
            using var arenaRepo = new Repository(arenaDir);
            var sig = new Signature("DiffBot", "diff@local", DateTimeOffset.Now);

            // 2A) Commit A: snapshot files from the baseline clone's worktree
            var baselineWorktreePath = baselineCloneDir;
            // copy everything EXCEPT its .git folder
            CopyFiles(baselineWorktreePath, arenaDir, excludeGitFolder: true);
            Commands.Stage(arenaRepo, "*");
            var commitA = arenaRepo.Commit("baseline snapshot", sig, sig);

            // clean working directory for next snapshot (keep .git)
            ResetWorkingTreeToEmpty(arenaDir);

            // 2B) Commit B: snapshot files from the target folder
            CopyFiles(request.TargetPath, arenaDir, excludeGitFolder: true);
            Commands.Stage(arenaRepo, "*");
            var commitB = arenaRepo.Commit("target snapshot", sig, sig);

            // 3) Diff inside the SAME repo (commitA -> commitB)
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

            // 4) Cleanup temp dirs
            TryDeleteDir(baselineCloneDir);
            TryDeleteDir(arenaDir);

            return await Task.FromResult(new DiffSummaryResponse(
                request.RemoteRepoUrl,
                request.TargetPath,
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

        private static void ValidatePaths(DiffSummaryRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.RemoteRepoUrl))
                throw new ArgumentException("RemoteRepoUrl must not be empty.");
            if (string.IsNullOrWhiteSpace(req.TargetPath) || !Directory.Exists(req.TargetPath))
                throw new DirectoryNotFoundException($"Target folder not found: {req.TargetPath}");
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
