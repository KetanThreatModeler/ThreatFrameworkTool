using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Core.Git;

namespace ThreatFramework.Git.Contract
{
    public interface IDiffSummaryService
    {
        Task<DiffSummaryResponse> CompareAsync(DiffSummaryRequest request, CancellationToken ct);
    }
}
