using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ThreatModeler.TF.Infra.Contract.AssistRuleIndex.Model;

namespace ThreatModeler.TF.Infra.Contract.AssistRuleIndex.TRC
{
    public interface ITRCAssistRuleIndexService
    {
        Task GenerateIndexAsync();
        Task GenerateIndexAsync(IEnumerable<Guid> libraryGuids);
        Task RefreshAsync();
        Task<int> GetIdByRelationshipGuidAsync(Guid relationshipGuid);
        Task<int> GetIdByResourceTypeValueAsync(string resourceTypeValue);
        Task<IReadOnlyList<AssistRuleIndexEntry>> GetResourceTypeValuesByLibraryGuidAsync(Guid libraryGuid);
        Task<IReadOnlyList<AssistRuleIndexEntry>> GetAllAsync();
        Task<int> GetMaxAssignedIdAsync();
    }
}
