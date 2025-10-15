using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Drift.Contract.MappingDriftService.Model;

namespace ThreatFramework.Drift.Contract.MappingDriftService.Dto
{
    public class ComponentThreatSRDriftDto
    {
        public Guid ComponentGuid { get; init; }
        public List<ThreatSRMapping> Added { get; init; } = new List<ThreatSRMapping>();
        public List<ThreatSRMapping> Removed { get; init; } = new List<ThreatSRMapping>();
    }
}
