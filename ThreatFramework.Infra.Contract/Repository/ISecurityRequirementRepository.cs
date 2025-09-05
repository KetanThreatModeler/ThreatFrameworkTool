using ThreatFramework.Core.Models.CoreEntities;

namespace ThreatFramework.Infrastructure.Interfaces.Repositories
{
    public interface ISecurityRequirementRepository
    {
        Task<IEnumerable<SecurityRequirement>> GetSecurityRequirementsByLibraryIdAsync(IEnumerable<Guid> libraryIds);
        Task<IEnumerable<SecurityRequirement>> GetReadOnlySecurityRequirementsAsync();
    }
}
