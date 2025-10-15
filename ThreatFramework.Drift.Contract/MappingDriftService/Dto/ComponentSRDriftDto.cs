using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatFramework.Drift.Contract.MappingDriftService.Dto
{
    public class ComponentSRDriftDto
    {
        public Guid ComponentGuid { get; init; }
        public List<Guid> Added { get; init; } = new List<Guid>();   // SRs present in A but not in B
        public List<Guid> Removed { get; init; } = new List<Guid>(); // SRs present in B but not in A
    }
}
