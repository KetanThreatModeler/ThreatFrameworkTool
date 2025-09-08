using ThreatFramework.Core.CoreEntities;

namespace ThreatFramework.Infra.Contract.Repository
{
    public interface ISecurityRequirementRepository
    {
        Task<IEnumerable<SecurityRequirement>> GetSecurityRequirementsByLibraryIdAsync(IEnumerable<Guid> libraryIds);
        Task<IEnumerable<SecurityRequirement>> GetReadOnlySecurityRequirementsAsync();
    }
}
