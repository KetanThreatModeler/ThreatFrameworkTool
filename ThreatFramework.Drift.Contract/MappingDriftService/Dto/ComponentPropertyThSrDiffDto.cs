using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Drift.Contract.MappingDriftService.Model;

namespace ThreatFramework.Drift.Contract.MappingDriftService.Dto
{
    public class ComponentPropertyThSrDiffDto
    {
        public Guid ComponentGuid { get; set; }
        public List<PropertyThreatSRMapping> PropertyMappingsAdded { get; set; } = new();
        public List<PropertyThreatSRMapping> PropertyMappingsRemoved { get; set; } = new();  

    }
}
