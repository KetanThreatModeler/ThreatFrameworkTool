using ThreatFramework.Drift.Contract.Model;
using ThreatModeler.TF.Drift.Contract.Model.UpdatedFinal;

namespace ThreatModeler.TF.Drift.Contract
{
    public interface IFinalDriftService
    {
        Task<TMFrameworkDrift> DriftAsync(IEnumerable<Guid> libraryIds, CancellationToken cancellationToken = default);
        Task<TMFrameworkDrift1> DriftAsync1(IEnumerable<Guid> libraryIds, CancellationToken cancellationToken = default);
    }
}
