using ThreatFramework.Infra.Contract.DataInsertion.Dto;

namespace ThreatFramework.Infra.Contract.DataInsertion
{
    public interface IGuidIntegrityService
    {
        Task<MissingGuidsByEntity> GetMissingGuidsAsync(CheckMissingGuidsRequest request);
    }
}
