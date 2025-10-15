using ThreatFramework.Infra.Contract.Index;
using ThreatFramework.Infra.Contract.Repository;
using ThreatFramework.Core.Config;

namespace ThreatFramework.Infrastructure.Index
{
    public class GuidSource : IGuidSource
    {
        private readonly IRepositoryHubFactory _hubFactory;

        public GuidSource(IRepositoryHubFactory hubFactory)
        {
            _hubFactory = hubFactory;
        }

        public async Task<IEnumerable<Guid>> GetAllGuidsAsync()
        {
            // Create TRC-scoped repository hub
            var hub = _hubFactory.Create(DataPlane.Trc);

            var tasks = new[]
            {
                hub.Libraries.GetLibraryGuidsAsync(),
                hub.Components.GetGuidsAsync(),
                hub.Threats.GetGuidsAsync(),
                hub.Testcases.GetGuidsAsync(),
                hub.SecurityRequirements.GetGuidAsync(),
                hub.Properties.GetGuidsAsync(),
                hub.PropertyOptions.GetAllPropertyOptionGuidsAsync()
            };

            var results = await Task.WhenAll(tasks);
            return results.SelectMany(guids => guids).ToHashSet();
        }
    }
}
