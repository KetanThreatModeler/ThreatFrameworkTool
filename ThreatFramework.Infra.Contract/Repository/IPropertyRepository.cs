using ThreatFramework.Core.CoreEntities;

namespace ThreatFramework.Infra.Contract.Repository
{
    public interface IPropertyRepository
    {
        Task<IEnumerable<Property>> GetPropertiesByLibraryIdAsync(IEnumerable<Guid> libraryIds);
        Task<IEnumerable<Property>> GetReadOnlyPropertiesAsync();
        Task<IEnumerable<Guid>> GetGuidsAsync();
        Task<IEnumerable<(Guid PropertyGuid, Guid LibraryGuid)>> GetGuidsAndLibraryGuidsAsync();
    }
}
