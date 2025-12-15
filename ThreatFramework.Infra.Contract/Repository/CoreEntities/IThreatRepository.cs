using ThreatModeler.TF.Core.Model.CoreEntities;

namespace ThreatModeler.TF.Infra.Contract.Repository.CoreEntities
{
    public interface IThreatRepository
    {
        Task<IEnumerable<Threat>> GetThreatsByLibraryIdAsync(IEnumerable<Guid> guids);
        Task<IEnumerable<Threat>> GetReadOnlyThreatsAsync();
        Task<IEnumerable<Guid>> GetGuidsAsync();
        Task<IEnumerable<(Guid ThreatGuid, Guid LibraryGuid)>> GetGuidsAndLibraryGuidsAsync();
        Task<IEnumerable<(Guid ThreatGuid, Guid LibraryGuid)>> GetGuidsAndLibraryGuidsAsync(IEnumerable<Guid> libraryIds);
        Task<IEnumerable<Guid>> GetGuidsByLibraryIds(IEnumerable<Guid> libraryIds);
    }
}
