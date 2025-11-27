using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatModeler.TF.Core.Global
{
    public class ComponentType
    {
        public Guid Guid { get; set; }
        public string Name { get; set; }    = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Guid LibraryGuid { get; set; }
        public bool IsHidden { get; set; }  
        public bool IsSecurityControl { get; set; }
        public string ChineseName { get; set; } = string.Empty;
        public string ChineseDescription { get; set; } = string.Empty;
    }
}
