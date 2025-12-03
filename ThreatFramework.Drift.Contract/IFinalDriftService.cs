using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Drift.Contract.Model;

namespace ThreatModeler.TF.Drift.Contract
{
    public interface IFinalDriftService
    {
        Task<TMFrameworkDrift> DriftAsync(IEnumerable<Guid> libraryIds, CancellationToken cancellationToken = default);
    }
}
