using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Drift.Contract.Model;

namespace ThreatFramework.Drift.Contract
{
    public interface IDriftService
    {
        Task<DriftAnalyzeResponse> AnalyzeAsync(
            DriftAnalyzeRequest request,
            CancellationToken ct = default);
    }
}
