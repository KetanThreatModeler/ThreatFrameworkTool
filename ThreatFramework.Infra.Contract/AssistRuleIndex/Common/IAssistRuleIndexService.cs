using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatModeler.TF.Infra.Contract.AssistRuleIndex.Common
{
    public interface IAssistRuleIndexService
    {
        Task RefreshAsync();
        Task<int> GetIdByRelationshipGuidAsync(Guid relationshipGuid);
        Task<int> GetIdByResourceTypeValueAsync(string resourceTypeValue);
    }
}
