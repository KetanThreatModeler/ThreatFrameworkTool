using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatModeler.TF.Infra.Contract.AssistRuleIndex.Model;

namespace ThreatModeler.TF.Infra.Contract.AssistRuleIndex.Service
{
    public interface IAssistRuleIndexQuery
    {
        bool TryGetIdByRelationshipGuid(Guid relationshipGuid, out string id);
        bool TryGetIdByResourceTypeValue(string resourceTypeValue, out string id);

        IReadOnlyList<AssistRuleIndexEntry> GetResourceTypeValuesByLibraryGuid(Guid libraryGuid);

        IReadOnlyList<AssistRuleIndexEntry> GetAll();
    }
}
