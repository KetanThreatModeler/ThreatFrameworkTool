using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatModeler.TF.Drift.Contract.MappingDriftService.Dto
{
    public class ThreatSRMappingDto
    {
        public ThreatSRMappingDto(Guid threaId, Guid? srId)
        {
            ThreatId = threaId;
            SRId = srId;
        }


        public ThreatSRMappingDto(Guid threaId)
        {
            ThreatId = threaId;
            SRId = null;
        }

        public Guid ThreatId { get; set; }
        public Guid? SRId { get; set; }
    }
}
