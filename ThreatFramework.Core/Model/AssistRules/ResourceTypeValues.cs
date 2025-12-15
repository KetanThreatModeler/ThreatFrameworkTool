using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatModeler.TF.Core.Model.AssistRules
{
    public class ResourceTypeValues
    {
        public string ResourceName { get; set; }

        public string ResourceTypeValue { get; set; }

        public Guid ComponentGuid { get; set; }

        public Guid LibraryId { get; set; }
    }
}
