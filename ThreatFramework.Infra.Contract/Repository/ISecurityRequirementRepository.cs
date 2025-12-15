using ThreatModeler.TF.Core.Model.CoreEntities;

namespace ThreatFramework.Infra.Contract.Repository
{
    public interface ISecurityRequirementRepository
    {
        Task<IEnumerable<SecurityRequirement>> GetSecurityRequirementsByLibraryIdAsync(IEnumerable<Guid> libraryIds);
        Task<IEnumerable<SecurityRequirement>> GetReadOnlySecurityRequirementsAsync();
        Task<IEnumerable<Guid>> GetGuidAsync();
        Task<IEnumerable<(Guid SecurityRequirementGuid, Guid LibraryGuid)>> GetGuidsAndLibraryGuidsAsync();
        Task<IEnumerable<(Guid SecurityRequirementGuid, Guid LibraryGuid)>> GetGuidsAndLibraryGuidsAsync(IEnumerable<Guid> libraryIds);
        Task<IEnumerable<Guid>> GetGuidsByLibraryIds(IEnumerable<Guid> libraryIds);
    }
}
