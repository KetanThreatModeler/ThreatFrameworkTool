using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatModeler.TF.Infra.Contract.AssistRuleIndex.Common.Model;

namespace ThreatModeler.TF.Infra.Contract.AssistRuleIndex.Common.Writer
{
    public interface IAssistRuleIndexSerializer
    {
        string Serialize(IReadOnlyList<AssistRuleIndexEntry> entries);
        IReadOnlyList<AssistRuleIndexEntry> Deserialize(string yaml);
    }
}
