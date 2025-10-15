using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatFramework.Drift.Contract.MappingDriftService.Model
{
    public class PropertyThreatSRMapping
    {
        public PropertyThreatSRMapping(Guid PropertId, Guid PropertyOptionId, Guid ThreatId, Guid SRId)
        {
            this.PropertyId = PropertId;
            this.PropertyOptionId = PropertyOptionId;
            this.ThreatId = ThreatId;
            this.SRId = SRId;
        }

        public Guid PropertyId { get; set;}
        public Guid PropertyOptionId { get; set;}
        public Guid ThreatId { get; set;}
        public Guid SRId { get; set;}
    }
}
