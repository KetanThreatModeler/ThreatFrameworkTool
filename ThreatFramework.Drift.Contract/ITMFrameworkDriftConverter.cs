using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Core.CoreEntities;
using ThreatFramework.Drift.Contract.MappingDriftService.Model;
using ThreatFramework.Drift.Contract.Model;
using ThreatModeler.TF.Core.Global;
using ThreatModeler.TF.Drift.Contract.MappingDriftService.Dto;
using ThreatModeler.TF.Drift.Contract.Model.UpdatedFinal;

namespace ThreatModeler.TF.Drift.Contract
{
    public interface ITMFrameworkDriftConverter
    {
        Task<TMFrameworkDrift1> ConvertAsync(TMFrameworkDrift source);
    }
}
