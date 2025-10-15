using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Drift.Contract.MappingDriftService.Dto;

namespace ThreatFramework.Drift.Contract.MappingDriftService
{
    public interface IComponentMappingDriftService
    {
        Task<IEnumerable<ComponentMappingDriftDto>> GetMappingDrift();
    }
}
