using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatModeler.TF.Infra.Contract.AssistRuleIndex.Common.Model;

namespace ThreatModeler.TF.Infra.Contract.AssistRuleIndex.TRC
{
    public interface ITRCAssistRuleIndexManager
    {
        Task<IReadOnlyList<AssistRuleIndexEntry>> BuildAndWriteAsync(
            IEnumerable<Guid> libraryGuids);

        Task<IReadOnlyList<AssistRuleIndexEntry>> BuildAndWriteAsync();

        Task<IReadOnlyList<AssistRuleIndexEntry>> ReloadFromYamlAsync();
    }
}
