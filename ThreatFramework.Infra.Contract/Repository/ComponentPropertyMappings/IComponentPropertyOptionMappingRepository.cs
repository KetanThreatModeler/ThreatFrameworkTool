using ThreatModeler.TF.Core.Model.PropertyMapping;

namespace ThreatFramework.Infra.Contract.Repository
{
    public interface IComponentPropertyOptionMappingRepository
    {

        Task<IEnumerable<ComponentPropertyOptionMapping>> GetMappingsByLibraryGuidAsync(IEnumerable<Guid> libraryGuidsds);
        Task<IEnumerable<ComponentPropertyOptionMapping>> GetReadOnlyMappingsAsync();
    }
}
