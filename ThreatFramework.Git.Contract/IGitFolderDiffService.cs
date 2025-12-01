using ThreatModeler.TF.Git.Contract.Models;

namespace ThreatModeler.TF.Git.Contract
{
    public interface IGitFolderDiffService
    {
        Task<FolderDiffReport> CompareFoldersAsync(
            string repo1Path,
            string repo2Path,
            List<string> foldersToCheck,
            bool includeUncommittedChanges = true);


        Task<FolderDiffReport> CompareByPrefixAsync(
            string repo1Path,
            string repo2Path,
            string folderPath,
            List<string> prefixes,
            bool includeUncommittedChanges = true);


        Task<FolderDiffReport> CompareRepoWithExclusionsAsync(
            string repo1Path,
            string repo2Path,
            List<string> filesToIgnore,
            bool includeUncommittedChanges = true);
    }
}
