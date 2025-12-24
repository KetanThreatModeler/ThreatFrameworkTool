using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatModeler.TF.Infra.Contract.Index.Client;
using ThreatModeler.TF.Infra.Contract.Index.Common;
using ThreatModeler.TF.Infra.Contract.Index.TRC;
            
namespace ThreatModeler.TF.Infra.Implmentation.Index.Common
{
    public class GuidIndexService : IGuidIndexService
    {
        private readonly ITRCGuidIndexService _trcGuidIndexService;
        private readonly IClientGuidIndexService _clientGuidIndexService;
        public GuidIndexService(ITRCGuidIndexService trcGuidIndexService, IClientGuidIndexService clientGuidIndexService)
        {
            _trcGuidIndexService = trcGuidIndexService;
            _clientGuidIndexService = clientGuidIndexService;
        }

        public async Task<Guid> GetGuidAsync(int id)
        {
            var guid = await _trcGuidIndexService.GetGuidAsync(id);
            if (guid == Guid.Empty)
            {
                guid = await _clientGuidIndexService.GetGuidAsync(id);
            }

            if (guid == Guid.Empty)
                throw new InvalidOperationException($"No GUID found for the provided ID : {id}.");
            return guid;
        }

        public async Task<int> GetIntAsync(Guid guid)
        {
            var id = await _trcGuidIndexService.GetIntAsync(guid);
            if (id == 0)
            {
                id = await _clientGuidIndexService.GetIntAsync(guid);
            }
            if (id == 0)
            {
                throw new InvalidOperationException($"No ID found for the provided GUID : {guid}.");
            }
            return id;
        }

        public async Task RefreshAsync()
        {
           await _trcGuidIndexService.RefreshAsync();
           await _clientGuidIndexService.RefreshAsync();
        }
    }
}
