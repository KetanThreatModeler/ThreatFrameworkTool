using ThreatFramework.Infra.Contract.Index;
using ThreatFramework.Infra.Contract.Repository;
using ThreatModeler.TF.Infra.Contract.Repository;

namespace ThreatFramework.Infrastructure.Index
{
    public class GuidSource : IGuidSource
    {
        private readonly IRepositoryHubFactory _hubFactory;

        public GuidSource(IRepositoryHubFactory hubFactory)
        {
            _hubFactory = hubFactory;
        }

        public async Task<IEnumerable<EntityIdentifier>> GetAllGuidsWithTypeAsync()
        {
            // Create TRC-scoped repository hub
            IRepositoryHub hub = _hubFactory.Create(DataPlane.Trc);

            Task<IEnumerable<EntityIdentifier>>[] tasks = new[]
            {
                GetLibraryIdentifiersAsync(hub),
                GetComponentIdentifiersAsync(hub),
                GetComponentTypeIdentifiersAsync(hub),
                GetThreatIdentifiersAsync(hub),
                GetTestcaseIdentifiersAsync(hub),
                GetSecurityRequirementIdentifiersAsync(hub),
                GetPropertyIdentifiersAsync(hub),
                GetPropertyTypeIdentifiersAsync(hub),
                GetPropertyOptionIdentifiersAsync(hub)
            };

            IEnumerable<EntityIdentifier>[] results = await Task.WhenAll(tasks);
            return results.SelectMany(identifiers => identifiers).ToList();
        }

        private async Task<IEnumerable<EntityIdentifier>> GetLibraryIdentifiersAsync(IRepositoryHub hub)
        {
            IEnumerable<Guid> guids = await hub.Libraries.GetLibraryGuidsAsync();
            return guids.Select(guid => new EntityIdentifier
            {
                Guid = guid,
                LibraryGuid = guid,
                EntityType = EntityType.Library
            });
        }

        private async Task<IEnumerable<EntityIdentifier>> GetComponentIdentifiersAsync(IRepositoryHub hub)
        {
            IEnumerable<(Guid ComponentGuid, Guid LibraryGuid)> guids = await hub.Components.GetGuidsAndLibraryGuidsAsync();
            return guids.Select(item => new EntityIdentifier
            {
                Guid = item.ComponentGuid,
                LibraryGuid = item.LibraryGuid,
                EntityType = EntityType.Component
            });
        }

        private async Task<IEnumerable<EntityIdentifier>> GetThreatIdentifiersAsync(IRepositoryHub hub)
        {
            try
            {
                IEnumerable<(Guid ThreatGuid, Guid LibraryGuid)> guids = await hub.Threats.GetGuidsAndLibraryGuidsAsync();
                return guids.Select(item => new EntityIdentifier
                {
                    Guid = item.ThreatGuid,
                    LibraryGuid = item.LibraryGuid,
                    EntityType = EntityType.Threat
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving threat identifiers: {ex.Message}");
                throw;

            }
        }

        private async Task<IEnumerable<EntityIdentifier>> GetTestcaseIdentifiersAsync(IRepositoryHub hub)
        {
            IEnumerable<(Guid TestCaseGuid, Guid LibraryGuid)> guids = await hub.Testcases.GetGuidsAndLibraryGuidsAsync();
            return guids.Select(item => new EntityIdentifier
            {
                Guid = item.TestCaseGuid,
                LibraryGuid = item.LibraryGuid,
                EntityType = EntityType.TestCase
            });
        }

        private async Task<IEnumerable<EntityIdentifier>> GetSecurityRequirementIdentifiersAsync(IRepositoryHub hub)
        {
            IEnumerable<(Guid SecurityRequirementGuid, Guid LibraryGuid)> guids = await hub.SecurityRequirements.GetGuidsAndLibraryGuidsAsync();
            return guids.Select(item => new EntityIdentifier
            {
                Guid = item.SecurityRequirementGuid,
                LibraryGuid = item.LibraryGuid,
                EntityType = EntityType.SecurityRequirement
            });
        }

        private async Task<IEnumerable<EntityIdentifier>> GetPropertyIdentifiersAsync(IRepositoryHub hub)
        {
            IEnumerable<(Guid PropertyGuid, Guid LibraryGuid)> guids = await hub.Properties.GetGuidsAndLibraryGuidsAsync();
            return guids.Select(item => new EntityIdentifier
            {
                Guid = item.PropertyGuid,
                LibraryGuid = item.LibraryGuid,
                EntityType = EntityType.Property
            });
        }

        private async Task<IEnumerable<EntityIdentifier>> GetPropertyOptionIdentifiersAsync(IRepositoryHub hub)
        {
            IEnumerable<Guid> guids = await hub.PropertyOptions.GetAllPropertyOptionGuidsAsync();
            return guids.Select(guid => new EntityIdentifier
            {
                Guid = guid,
                LibraryGuid = Guid.Empty,
                EntityType = EntityType.PropertyOption  
            });
        }

        private async Task<IEnumerable<EntityIdentifier>> GetPropertyTypeIdentifiersAsync(IRepositoryHub hub)
        {
            IEnumerable<Guid> guids = await hub.PropertyTypes.GetAllPropertyTypeGuidsAsync();
            return guids.Select(guid => new EntityIdentifier
            {
                Guid = guid,
                LibraryGuid = Guid.Empty,
                EntityType = EntityType.PropertyType  
            });
        }

        private async Task<IEnumerable<EntityIdentifier>> GetComponentTypeIdentifiersAsync(IRepositoryHub hub)
        {
            IEnumerable<(Guid ComponentTypeGuid, Guid LibraryGuid)> guids = await hub.ComponentTypes.GetGuidsAndLibraryGuidsAsync();
            return guids.Select(item => new EntityIdentifier
            {
                Guid = item.ComponentTypeGuid,
                LibraryGuid = item.LibraryGuid,
                EntityType = EntityType.ComponentType
            });
        }
    }
}
