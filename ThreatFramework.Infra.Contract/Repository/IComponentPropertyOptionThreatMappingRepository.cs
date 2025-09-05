using ThreatFramework.Core.Models.PropertyMapping;

namespace ThreatFramework.Infrastructure.Interfaces.Repositories
{
    public interface IComponentPropertyOptionThreatMappingRepository
    {
        Task<IEnumerable<ComponentPropertyOptionThreatMapping>> GetAllComponentPropertyOptionThreatMappingsAsync();
    }
}
