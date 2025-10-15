using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Core;
using ThreatFramework.Drift.Contract.CoreEntityDriftService.Model;
using ThreatFramework.Drift.Contract.MappingDriftService.Dto;
using ThreatFramework.Drift.Contract.Model;

namespace ThreatFramework.Drift.Contract
{
    public interface ILibraryDriftAggregator
    {
       Task<TMFrameworkDrift> Drift();
       Task<TMFrameworkDrift> Aggregate(CoreEntitiesDrift report, IEnumerable<ComponentMappingDriftDto> mappingDrift);
    }
}
