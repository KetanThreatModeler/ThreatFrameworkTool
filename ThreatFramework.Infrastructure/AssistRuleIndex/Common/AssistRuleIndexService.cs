using System;
using System.Threading.Tasks;
using ThreatModeler.TF.Infra.Contract.AssistRuleIndex.Client;
using ThreatModeler.TF.Infra.Contract.AssistRuleIndex.Common;
using ThreatModeler.TF.Infra.Contract.AssistRuleIndex.TRC;

namespace ThreatModeler.TF.Infra.Implmentation.AssistRuleIndex.Common
{
    public class AssistRuleIndexService : IAssistRuleIndexService
    {
        private readonly ITRCAssistRuleIndexService _trcAssistRuleIndexService;
        private readonly IClientAssistRuleIndexService _clientAssistRuleIndexService;

        public AssistRuleIndexService(
            ITRCAssistRuleIndexService trcAssistRuleIndexService,
            IClientAssistRuleIndexService clientAssistRuleIndexService)
        {
            _trcAssistRuleIndexService = trcAssistRuleIndexService ?? throw new ArgumentNullException(nameof(trcAssistRuleIndexService));
            _clientAssistRuleIndexService = clientAssistRuleIndexService ?? throw new ArgumentNullException(nameof(clientAssistRuleIndexService));
        }

        public async Task<int> GetIdByRelationshipGuidAsync(Guid relationshipGuid)
        {
            if (relationshipGuid == Guid.Empty)
                throw new ArgumentException("Relationship GUID cannot be empty.", nameof(relationshipGuid));

            var id = await _trcAssistRuleIndexService.GetIdByRelationshipGuidAsync(relationshipGuid);
            if (id == 0)
            {
                id = await _clientAssistRuleIndexService.GetIdByRelationshipGuidAsync(relationshipGuid);
            }

            if (id == 0)
                throw new InvalidOperationException($"No ID found for the provided Relationship GUID : {relationshipGuid}.");

            return id;
        }

        public async Task<int> GetIdByResourceTypeValueAsync(string resourceTypeValue)
        {
            if (string.IsNullOrWhiteSpace(resourceTypeValue))
                throw new ArgumentException("Resource type value cannot be null/empty.", nameof(resourceTypeValue));

            var id = await _trcAssistRuleIndexService.GetIdByResourceTypeValueAsync(resourceTypeValue);
            if (id == 0)
            {
                id = await _clientAssistRuleIndexService.GetIdByResourceTypeValueAsync(resourceTypeValue);
            }

            if (id == 0)
                throw new InvalidOperationException($"No ID found for the provided ResourceTypeValue : '{resourceTypeValue}'.");

            return id;
        }

        public async Task RefreshAsync()
        {
            await _trcAssistRuleIndexService.RefreshAsync();
            await _clientAssistRuleIndexService.RefreshAsync();
        }
    }
}
