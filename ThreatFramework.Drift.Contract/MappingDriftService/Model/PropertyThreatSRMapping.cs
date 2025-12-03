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
        public string PropertyName { get; set; } = string.Empty;
        public Guid PropertyOptionId { get; set;}
        public string PropertyOptionName { get; set; } = string.Empty;
        public Guid ThreatId { get; set;}
        public string ThreatName { get; set; } = string.Empty;
        public Guid SRId { get; set;}
        public string SRName { get; set; } = string.Empty;
    }
}
