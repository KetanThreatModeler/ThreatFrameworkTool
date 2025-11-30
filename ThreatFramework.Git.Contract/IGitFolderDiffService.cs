using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatModeler.TF.Git.Contract.Models;

namespace ThreatModeler.TF.Git.Contract
{
    public interface IGitFolderDiffService
    {
        /// <summary>
        /// Compares specific folders across two repositories.
        /// Repo1 = New/Source, Repo2 = Old/Target.
        /// </summary>
        Task<FolderDiffReport> CompareFoldersAsync(string repo1Path, string repo2Path, List<string> foldersToCheck);

        Task<FolderDiffReport> CompareByPrefixAsync(
            string repo1Path,
            string repo2Path,
            string folderPath,
            List<string> prefixes);

        Task<FolderDiffReport> CompareRepoWithExclusionsAsync(
            string repo1Path,
            string repo2Path,
            List<string> filesToIgnore);
    }
}

