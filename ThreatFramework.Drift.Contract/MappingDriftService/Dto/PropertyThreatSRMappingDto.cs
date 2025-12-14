using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatModeler.TF.Drift.Contract.MappingDriftService.Dto
{
    public class PropertyThreatSRMappingDto
    {
        public PropertyThreatSRMappingDto(Guid PropertId, Guid PropertyOptionId, Guid ThreatId, Guid SRId)
        {
            PropertyId = PropertId;
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
