using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Core.Git;

namespace ThreatFramework.Git.Contract
{
    public interface IFolderToFolderDiffService
    {
        Task<DiffSummaryResponse> CompareAsync(FolderToFolderDiffRequest request, CancellationToken ct);
    }
}
