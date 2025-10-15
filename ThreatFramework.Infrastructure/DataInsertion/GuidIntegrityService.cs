using ThreatFramework.Infra.Contract.DataInsertion;
using ThreatFramework.Infra.Contract.DataInsertion.Dto;

namespace ThreatFramework.Infrastructure.DataInsertion
{
    public sealed class GuidIntegrityService : IGuidIntegrityService
    {
        private readonly IGuidLookupRepository _repository;
        public GuidIntegrityService(IGuidLookupRepository repository)
        {
            _repository = repository;
        }

        public Task<MissingGuidsByEntity> GetMissingGuidsAsync(CheckMissingGuidsRequest request)
        {
            return _repository.GetMissingGuidsAsync(request);
        }
    }
}
