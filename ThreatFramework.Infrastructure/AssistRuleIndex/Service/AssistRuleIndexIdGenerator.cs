using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatModeler.TF.Infra.Contract.AssistRuleIndex.Service;

namespace ThreatModeler.TF.Infra.Implmentation.AssistRuleIndex.Service
{
    public sealed class AssistRuleIndexIdGenerator : IAssistRuleIndexIdGenerator
    {
        private readonly ConcurrentDictionary<string, int> _counters = new();

        public void Reset() => _counters.Clear();

        public string Next(string prefix)
        {
            var next = _counters.AddOrUpdate(prefix, 1, (_, current) => current + 1);
            return $"{prefix}_{next}";
        }
    }
}