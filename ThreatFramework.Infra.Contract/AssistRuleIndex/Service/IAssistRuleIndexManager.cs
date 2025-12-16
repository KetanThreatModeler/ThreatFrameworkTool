using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatModeler.TF.Infra.Contract.AssistRuleIndex.Model;

namespace ThreatModeler.TF.Infra.Contract.AssistRuleIndex.Service
{
    public interface IAssistRuleIndexManager
    {
        Task<IReadOnlyList<AssistRuleIndexEntry>> BuildAsync(IEnumerable<Guid> libraryGuids);

        Task<IReadOnlyList<AssistRuleIndexEntry>> BuildAndWriteAsync(
            IEnumerable<Guid> libraryGuids);

        Task ReloadFromYamlAsync();
    }
}
