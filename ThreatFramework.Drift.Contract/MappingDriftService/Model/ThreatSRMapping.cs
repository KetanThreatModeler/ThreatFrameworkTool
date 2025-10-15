using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatFramework.Drift.Contract.MappingDriftService.Model
{
    public class ThreatSRMapping
    {
        public ThreatSRMapping(Guid threaId, Guid? srId)
        {
            ThreatId = threaId;
            SRId = srId;
        }


        public ThreatSRMapping(Guid threaId)
        {
            ThreatId = threaId;
            SRId = null;
        }

        public Guid ThreatId { get; set; }
        public Guid? SRId { get; set; }
    }
}
