using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatModeler.TF.Core.Model.CoreEntities
{
    public sealed class PathOptions
    {
        public const string SectionName = "Path";
        public string IndexYaml { get; set; } = string.Empty;
        public string TrcOutput { get; set; } = string.Empty;
        public string ClientOutput { get; set; } = string.Empty;
        public string AssistRuleIndexYaml { get; set; } = string.Empty;
    }
}
