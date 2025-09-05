using ThreatFramework.Core.Models.CoreEntities;

namespace ThreatFramework.Infra.Contract.Repository
{
    public interface IPropertyRepository
    {
        Task<IEnumerable<Property>> GetPropertiesByLibraryIdAsync(IEnumerable<Guid> libraryIds);
        Task<IEnumerable<Property>> GetReadOnlyPropertiesAsync();
    }
}
