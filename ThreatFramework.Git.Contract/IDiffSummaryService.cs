using ThreatFramework.Core.Git;

namespace ThreatFramework.Git.Contract
{
    public interface IDiffSummaryService
    {
        Task<DiffSummaryResponse> CompareAsync(DiffSummaryRequest request, CancellationToken ct);
    }
}
