using ThreatFramework.Core.Models.PropertyMapping;

namespace ThreatFramework.Infrastructure.Interfaces.Repositories
{
    public interface IComponentPropertyOptionThreatSecurityRequirementMappingRepository
    {
        Task<IEnumerable<ComponentPropertyOptionThreatSecurityRequirementMapping>> GetAllComponentPropertyOptionThreatSecurityRequirementMappingsAsync();
    }
}
