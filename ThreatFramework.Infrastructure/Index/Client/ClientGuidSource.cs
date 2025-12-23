using Microsoft.Extensions.Logging;
using ThreatFramework.Infra.Contract.Index;
using ThreatFramework.Infra.Contract.Repository;
using ThreatModeler.TF.Infra.Contract.Index.Client;
using ThreatModeler.TF.Infra.Contract.Repository;
using ThreatModeler.TF.Infra.Implmentation.Index.Common;


namespace ThreatModeler.TF.Infra.Implmentation.Index.Client
{
    public class ClientGuidSource : GuidSource, IClientGuidSource
    {
        private readonly IRepositoryHubFactory _hubFactory;
        private readonly ILogger<ClientGuidSource> _logger;

        public ClientGuidSource(IRepositoryHubFactory hubFactory, ILogger<ClientGuidSource> logger)
        {
            _hubFactory = hubFactory ?? throw new ArgumentNullException(nameof(hubFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<EntityIdentifier>> GetAllGuidsWithTypeAsyncForClient()
        {
            using (_logger.BeginScope("Operation: GetAllGuidsWithType"))
            {
                _logger.LogInformation("Starting retrieval of ALL entity identifiers.");

                // Create TRC-scoped repository hub
                IRepositoryHub hub = _hubFactory.Create(DataPlane.Client);

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

        public async Task<IEnumerable<EntityIdentifier>> GetGuidsWithTypeByLibraryIdsAsyncForClient(IEnumerable<Guid> libraryIds)
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

                IRepositoryHub hub = _hubFactory.Create(DataPlane.Client);

                // Strategy: Use the specific SQL-Filtering methods provided in the interface.
                // Note: Global entities are EXCLUDED here because they do not belong to specific libraries.
                var tasks = new List<Task<IEnumerable<EntityIdentifier>>>
                {
                    // A. Entities belonging to these libraries
                    FetchAllScopedAsync(hub, repo => repo.Components.GetGuidsAndLibraryGuidsAsync(libIdList), EntityType.Component),
                    FetchAllScopedAsync(hub, repo => repo.Threats.GetGuidsAndLibraryGuidsAsync(libIdList), EntityType.Threat),
                    FetchAllScopedAsync(hub, repo => repo.Testcases.GetGuidsAndLibraryGuidsAsync(libIdList), EntityType.TestCase),
                    FetchAllScopedAsync(hub, repo => repo.SecurityRequirements.GetGuidsAndLibraryGuidsAsync(libIdList), EntityType.SecurityRequirement),
                    FetchAllScopedAsync(hub, repo => repo.Properties.GetGuidsAndLibraryGuidsAsync(), EntityType.Property),
                    
                    // B. The Libraries themselves
                    FetchAllScopedAsync(hub, repo => repo.Libraries.GetLibraryGuidsWithLibGuidAsync(), EntityType.Library),

                    FetchAllGlobalAsync(hub, repo => repo.PropertyTypes.GetAllPropertyTypeGuidsAsync(), EntityType.PropertyType),
                    FetchAllGlobalAsync(hub, repo => repo.PropertyOptions.GetAllPropertyOptionGuidsAsync(), EntityType.PropertyOption),
                    FetchAllGlobalAsync(hub, repo => repo.ComponentTypes.GetGuidsAndLibraryGuidsAsync(), EntityType.ComponentType)
                };

                return await ExecuteAndAggregateAsync(tasks);
            }
        }
    }
}
