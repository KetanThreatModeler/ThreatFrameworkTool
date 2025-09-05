using ThreatFramework.Core.Models.PropertyMapping;

namespace ThreatFramework.Infrastructure.Interfaces.Repositories
{
    public interface IComponentPropertyOptionMappingRepository
    {
        Task<IEnumerable<ComponentPropertyOptionMapping>> GetAllComponentPropertyOptionMappingsAsync();
    }
}
