using ThreatFramework.Core.Models.ComponentMapping;

namespace ThreatFramework.Infrastructure.Interfaces.Repositories
{
    public interface IComponentSecurityRequirementMappingRepository
    {
        Task<IEnumerable<ComponentSecurityRequirementMapping>> GetAllComponentSecurityRequirementMappingsAsync();
    }
}
