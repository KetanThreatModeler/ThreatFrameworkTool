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
            var guid = await _trcGuidIndexService.GetGuidWithoutThrowingError(id);
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
            var id = await _trcGuidIndexService.GetIntForClientIndexGenerationAsync(guid);
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

        public async Task<Guid> ResolveLibraryGuidForComponentAsync(int componentId)
        {
            var trcEntity = await _trcGuidIndexService.GetIdentifierByIdAsync(componentId);
            if (trcEntity != null)
            {
                return trcEntity.LibraryGuid;
            }

            var clientEntity =  await _clientGuidIndexService.GetIdentifierByIdAsync(componentId);
            if (clientEntity != null)
            {
                return clientEntity.LibraryGuid;
            }
            throw new InvalidOperationException($"No Library GUID found for the provided component ID: {componentId}.");
        }
         
        public async Task<Guid> ResolveLibraryGuidForThreatAsync(int threatId)
        {
            var trcEntity = await _trcGuidIndexService.GetIdentifierByIdAsync(threatId);
            if (trcEntity != null)
            {
                return trcEntity.LibraryGuid;
            }

            var clientEntity = await _clientGuidIndexService.GetIdentifierByIdAsync(threatId);
            if (clientEntity != null)
            {
                return clientEntity.LibraryGuid;
            }
            throw new InvalidOperationException($"No Library GUID found for the provided threat ID: {threatId}.");
        }
    }
}
