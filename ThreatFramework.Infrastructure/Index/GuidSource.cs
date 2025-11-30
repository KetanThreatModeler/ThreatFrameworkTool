using Microsoft.Extensions.Logging;
using ThreatFramework.Infra.Contract.Index;
using ThreatFramework.Infra.Contract.Repository;
using ThreatModeler.TF.Infra.Contract.Repository;

namespace ThreatFramework.Infrastructure.Index
{
    public class GuidSource : IGuidSource
    {
        private readonly IRepositoryHubFactory _hubFactory;
        private readonly ILogger<GuidSource> _logger;

        public GuidSource(IRepositoryHubFactory hubFactory, ILogger<GuidSource> logger)
        {
            _hubFactory = hubFactory ?? throw new ArgumentNullException(nameof(hubFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // --------------------------------------------------------------------------------
        // 1. Get ALL Guids (Used for Global Index Generation)
        // --------------------------------------------------------------------------------
        public async Task<IEnumerable<EntityIdentifier>> GetAllGuidsWithTypeAsync()
        {
            using (_logger.BeginScope("Operation: GetAllGuidsWithType"))
            {
                _logger.LogInformation("Starting retrieval of ALL entity identifiers.");

                // Create TRC-scoped repository hub
                IRepositoryHub hub = _hubFactory.Create(DataPlane.Trc);

                // Strategy: Use methods that return (Guid, LibraryGuid) tuples
                var tasks = new List<Task<IEnumerable<EntityIdentifier>>>
                {
                    // A. Library-Scoped Entities
                    FetchAllScopedAsync(hub, repo => repo.Components.GetGuidsAndLibraryGuidsAsync(), EntityType.Component),
                    FetchAllScopedAsync(hub, repo => repo.Threats.GetGuidsAndLibraryGuidsAsync(), EntityType.Threat),
                    FetchAllScopedAsync(hub, repo => repo.Testcases.GetGuidsAndLibraryGuidsAsync(), EntityType.TestCase),
                    FetchAllScopedAsync(hub, repo => repo.SecurityRequirements.GetGuidsAndLibraryGuidsAsync(), EntityType.SecurityRequirement),
                    FetchAllScopedAsync(hub, repo => repo.Properties.GetGuidsAndLibraryGuidsAsync(), EntityType.Property),
                    
                    // B. Library Entities (Adapter needed: ID is both Guid and LibraryGuid)
                    FetchAllScopedAsync(hub, async repo =>
                    {
                        var libs = await repo.Libraries.GetLibraryGuidsAsync();
                        return libs.Select(g => (Id: g, LibId: g));
                    }, EntityType.Library),

                    // C. Global Entities (No Library Scope)
                    FetchAllGlobalAsync(hub, repo => repo.PropertyTypes.GetAllPropertyTypeGuidsAsync(), EntityType.PropertyType),
                    FetchAllGlobalAsync(hub, repo => repo.PropertyOptions.GetAllPropertyOptionGuidsAsync(), EntityType.PropertyOption),
                    FetchAllGlobalAsync(hub, repo => repo.ComponentTypes.GetGuidsAndLibraryGuidsAsync(), EntityType.ComponentType) 
                        
                };

                return await ExecuteAndAggregateAsync(tasks);
            }
        }

        // --------------------------------------------------------------------------------
        // 2. Get Filtered Guids (Used for Partial/Library Index Generation)
        // --------------------------------------------------------------------------------
        public async Task<IEnumerable<EntityIdentifier>> GetGuidsWithTypeByLibraryIdsAsync(IEnumerable<Guid> libraryIds)
        {
            using (_logger.BeginScope("Operation: GetGuidsWithTypeByLibraryIds"))
            {
                var libIdList = libraryIds?.ToList() ?? new List<Guid>();

                if (!libIdList.Any())
                {
                    _logger.LogWarning("No library IDs provided. Returning empty result set.");
                    return Enumerable.Empty<EntityIdentifier>();
                }

                _logger.LogInformation("Starting SQL-filtered retrieval for {Count} libraries.", libIdList.Count);

                IRepositoryHub hub = _hubFactory.Create(DataPlane.Trc);

                // Strategy: Use the specific SQL-Filtering methods provided in the interface.
                // Note: Global entities are EXCLUDED here because they do not belong to specific libraries.
                var tasks = new List<Task<IEnumerable<EntityIdentifier>>>
                {
                    // A. Entities belonging to these libraries
                    FetchFilteredAsync(hub, repo => repo.Components.GetGuidsByLibraryIds(libIdList), EntityType.Component, true),
                    FetchFilteredAsync(hub, repo => repo.Threats.GetGuidsByLibraryIds(libIdList), EntityType.Threat, true),
                    FetchFilteredAsync(hub, repo => repo.Testcases.GetGuidsByLibraryIds(libIdList), EntityType.TestCase, true),
                    FetchFilteredAsync(hub, repo => repo.SecurityRequirements.GetGuidsByLibraryIds(libIdList), EntityType.SecurityRequirement, true),
                    FetchFilteredAsync(hub, repo => repo.Properties.GetGuidsByLibraryIds(libIdList), EntityType.Property, true),
                    
                    // B. The Libraries themselves
                    FetchFilteredAsync(hub, repo => repo.Libraries.GetGuidsByLibraryIds(libIdList), EntityType.Library, isLibraryEntity: true),

                    FetchAllGlobalAsync(hub, repo => repo.PropertyTypes.GetAllPropertyTypeGuidsAsync(), EntityType.PropertyType),
                    FetchAllGlobalAsync(hub, repo => repo.PropertyOptions.GetAllPropertyOptionGuidsAsync(), EntityType.PropertyOption),
                    FetchAllGlobalAsync(hub, repo => repo.ComponentTypes.GetGuidsAndLibraryGuidsAsync(), EntityType.ComponentType)
                };

                return await ExecuteAndAggregateAsync(tasks);
            }
        }

        // --------------------------------------------------------------------------------
        // Shared Orchestration
        // --------------------------------------------------------------------------------
        private async Task<IEnumerable<EntityIdentifier>> ExecuteAndAggregateAsync(List<Task<IEnumerable<EntityIdentifier>>> tasks)
        {
            try
            {
                var results = await Task.WhenAll(tasks);
                var aggregated = results.SelectMany(identifiers => identifiers).ToList();

                _logger.LogInformation("Data retrieval completed. Total entities fetched: {Count}", aggregated.Count);
                return aggregated;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical failure during parallel data retrieval.");
                throw;
            }
        }

        // --------------------------------------------------------------------------------
        // DRY Helper Methods (The Engine)
        // --------------------------------------------------------------------------------

        /// <summary>
        /// 1. Fetches All Data (Tuple version): Returns accurate LibraryGuid mapping.
        /// </summary>
        private async Task<IEnumerable<EntityIdentifier>> FetchAllScopedAsync(
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
                LogFetchError(ex, type);
                throw;
            }
        }

        /// <summary>
        /// 2. Fetches Global Data (Guid only): LibraryGuid is Empty.
        /// </summary>
        private async Task<IEnumerable<EntityIdentifier>> FetchAllGlobalAsync(
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
                LogFetchError(ex, type);
                throw;
            }
        }

        /// <summary>
        /// 3. Fetches Filtered Data (Guid only): Uses the new SQL-Optimized Repository methods.
        /// </summary>
        private async Task<IEnumerable<EntityIdentifier>> FetchFilteredAsync(
            IRepositoryHub hub,
            Func<IRepositoryHub, Task<IEnumerable<Guid>>> fetchAction,
            EntityType type,
            bool isLibraryEntity = false)
        {
            try
            {
                // This calls the SQL method: GetGuidsByLibraryIds(...)
                var guids = await fetchAction(hub);

                return guids.Select(guid => new EntityIdentifier
                {
                    Guid = guid,
                    // LIMITATION: The 'GetGuidsByLibraryIds' interface only returns the Entity Guid, 
                    // not the parent Library Guid.
                    // If this is the Library itself, the ID *is* the LibraryGuid.
                    // Otherwise, we must leave it Empty or inferred by context downstream.
                    LibraryGuid = isLibraryEntity ? guid : Guid.Empty,
                    EntityType = type
                });
            }
            catch (Exception ex)
            {
                LogFetchError(ex, type);
                throw;
            }
        }

        private void LogFetchError(Exception ex, EntityType type)
        {
            _logger.LogError(ex, "Failed to retrieve identifiers for EntityType: {EntityType}", type);
        }
    }
}