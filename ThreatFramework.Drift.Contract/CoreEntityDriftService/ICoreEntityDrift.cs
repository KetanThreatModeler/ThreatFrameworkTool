using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Drift.Contract.CoreEntityDriftService.Model;
using ThreatFramework.Drift.Contract.FolderDiff;
using ThreatFramework.Drift.Contract.Model;

namespace ThreatFramework.Drift.Contract.CoreEntityDriftService
{
    public interface ICoreEntityDrift
    {
        Task<CoreEntitiesDrift> BuildAsync(FolderComparisionResult request);
    }
}
