using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatModeler.TF.Infra.Contract.AssistRuleIndex.Service
{
    public interface IAssistRuleIndexIdGenerator
    {
        void Reset();
        string Next(string prefix);
    }
}
