using ThreatModeler.TF.Drift.Contract.Dto;
using ThreatModeler.TF.Drift.Contract.Model;

namespace ThreatModeler.TF.Drift.Contract
{
    public interface IDriftService
    {
        Task<TMFrameworkDriftDto> DriftAsync(IEnumerable<Guid> libraryIds, CancellationToken cancellationToken = default);
        Task<TMFrameworkDrift> DriftAsync1(IEnumerable<Guid> libraryIds, CancellationToken cancellationToken = default);
    }
}
