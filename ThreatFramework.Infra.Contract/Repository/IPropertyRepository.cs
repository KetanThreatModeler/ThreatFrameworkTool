using ThreatModeler.TF.Core.Model.CoreEntities;

namespace ThreatFramework.Infra.Contract.Repository
{
    public interface IPropertyRepository
    {
        Task<IEnumerable<Property>> GetPropertiesByLibraryIdAsync(IEnumerable<Guid> libraryIds);
        Task<IEnumerable<Property>> GetReadOnlyPropertiesAsync();
        Task<IEnumerable<Guid>> GetGuidsAsync();
        Task<IEnumerable<(Guid PropertyGuid, Guid LibraryGuid)>> GetGuidsAndLibraryGuidsAsync();

        Task<IEnumerable<(Guid PropertyGuid, Guid LibraryGuid)>> GetGuidsAndLibraryGuidsAsync(IEnumerable<Guid> libraryIds);
        Task<IEnumerable<Guid>> GetGuidsByLibraryIds(IEnumerable<Guid> libraryIds);
    }
}
