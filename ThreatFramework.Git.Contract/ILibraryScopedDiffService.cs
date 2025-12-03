using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatModeler.TF.Git.Contract.Models;

namespace ThreatModeler.TF.Git.Contract
{
    public interface ILibraryScopedDiffService
    {
        Task<FolderDiffReport> CompareLibrariesAsync(
            string baseRepositoryPath,
            string targetRepositoryPath,
            IEnumerable<Guid> libraryGuids,
            bool includeUncommittedChanges = true);
    }
}
