using ThreatFramework.Core.Models.CoreEntities;

namespace ThreatFramework.Infrastructure.Interfaces.Repositories
{
    public interface IPropertyRepository
    {
        Task<IEnumerable<Property>> GetPropertiesByLibraryIdAsync(IEnumerable<Guid> libraryIds);
        Task<IEnumerable<Property>> GetReadOnlyPropertiesAsync();
    }
}
