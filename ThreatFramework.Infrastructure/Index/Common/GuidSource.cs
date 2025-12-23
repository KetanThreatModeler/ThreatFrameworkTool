using ThreatFramework.Infra.Contract.Index;
using ThreatModeler.TF.Infra.Contract.Repository;


namespace ThreatModeler.TF.Infra.Implmentation.Index.Common
{
    public class GuidSource
    {
        public async Task<IEnumerable<EntityIdentifier>> ExecuteAndAggregateAsync(List<Task<IEnumerable<EntityIdentifier>>> tasks)
        {
            try
            {
                var results = await Task.WhenAll(tasks);
                var aggregated = results.SelectMany(identifiers => identifiers).ToList();

                return aggregated;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        /// <summary>
        /// 1. Fetches All Data (Tuple version): Returns accurate LibraryGuid mapping.
        /// </summary>
        public async Task<IEnumerable<EntityIdentifier>> FetchAllScopedAsync(
            IRepositoryHub hub,
            Func<IRepositoryHub, Task<IEnumerable<(Guid Id, Guid LibId)>>> fetchAction,
            EntityType type)
        {
            try
            {
                var data = await fetchAction(hub);
                return data.Select(item => new EntityIdentifier
                {
                    Guid = item.Id,
                    LibraryGuid = item.LibId,
                    EntityType = type
                });
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// 2. Fetches Global Data (Guid only): LibraryGuid is Empty.
        /// </summary>
        public async Task<IEnumerable<EntityIdentifier>> FetchAllGlobalAsync(
            IRepositoryHub hub,
            Func<IRepositoryHub, Task<IEnumerable<Guid>>> fetchAction,
            EntityType type)
        {
            try
            {
                var data = await fetchAction(hub);
                return data.Select(guid => new EntityIdentifier
                {
                    Guid = guid,
                    LibraryGuid = Guid.Empty,
                    EntityType = type
                });
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
