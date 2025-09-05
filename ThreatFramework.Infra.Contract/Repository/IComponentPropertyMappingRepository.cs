using ThreatFramework.Core.Models.PropertyMapping;

namespace ThreatFramework.Infrastructure.Interfaces.Repositories
{
    public interface IComponentPropertyMappingRepository
    {
        Task<IEnumerable<ComponentPropertyMapping>> GetAllComponentPropertyMappingsAsync();
    }
}
