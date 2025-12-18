using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using ThreatFramework.Infra.Contract;
using ThreatFramework.Infra.Contract.Repository;
using ThreatModeler.TF.Core.Model.ComponentMapping;

namespace ThreatFramework.Infrastructure.Repository
{
    public class ComponentThreatSecurityRequirementMappingRepository : IComponentThreatSecurityRequirementMappingRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;
        private readonly ILibraryCacheService _libraryCacheService;

        public ComponentThreatSecurityRequirementMappingRepository(
            ISqlConnectionFactory connectionFactory,
            ILibraryCacheService libraryCacheService)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _libraryCacheService = libraryCacheService ?? throw new ArgumentNullException(nameof(libraryCacheService));
        }

        public async Task<IEnumerable<ComponentThreatSecurityRequirementMapping>> GetMappingsByLibraryIdAsync(IEnumerable<Guid> libraryGuids)
        {
            if (libraryGuids == null)
                throw new ArgumentNullException(nameof(libraryGuids));

            var libraryIds = await _libraryCacheService.GetIdsFromGuid(libraryGuids).ConfigureAwait(false);

            if (libraryIds == null || !libraryIds.Any())
                return Enumerable.Empty<ComponentThreatSecurityRequirementMapping>();

            return await GetByLibraryIntIdsAsync(libraryIds).ConfigureAwait(false);
        }

        public async Task<IEnumerable<ComponentThreatSecurityRequirementMapping>> GetReadOnlyMappingsAsync()
        {
            var readonlyLibraryIds = await _libraryCacheService.GetReadOnlyLibraryIdAsync().ConfigureAwait(false);

            if (readonlyLibraryIds == null || !readonlyLibraryIds.Any())
                return Enumerable.Empty<ComponentThreatSecurityRequirementMapping>();

            return await GetByLibraryIntIdsAsync(readonlyLibraryIds).ConfigureAwait(false);
        }

        // --------------------------
        // Private helpers
        // --------------------------

        private async Task<IEnumerable<ComponentThreatSecurityRequirementMapping>> GetByLibraryIntIdsAsync(IEnumerable<int> libraryIds)
        {
            var libraryIdList = libraryIds as IList<int> ?? libraryIds.ToList();

            if (libraryIdList.Count == 0)
                return Enumerable.Empty<ComponentThreatSecurityRequirementMapping>();

            var libraryParameters = string.Join(",", libraryIdList.Select((_, i) => $"@lib{i}"));

            var sql = $@"
{BuildMappingSelectQuery()}
WHERE
    (t.LibraryId IN ({libraryParameters}) OR c.LibraryId IN ({libraryParameters}))
    AND m.ComponentId IS NOT NULL
    AND m.ThreatId IS NOT NULL
    AND m.SecurityRequirementId IS NOT NULL
    AND m.PropertyId IS NULL
    AND m.PropertyOptionId IS NULL;";

            using var connection = await _connectionFactory.CreateOpenConnectionAsync().ConfigureAwait(false);
            using var command = new SqlCommand(sql, connection);

            for (int i = 0; i < libraryIdList.Count; i++)
            {
                command.Parameters.AddWithValue($"@lib{i}", libraryIdList[i]);
            }

            return await ExecuteMappingReaderAsync(command).ConfigureAwait(false);
        }

        private static string BuildMappingSelectQuery()
        {
            // Unified mapping table:
            // ComponentPropertyOptionThreatSecurityRequirementMapping
            return @"
SELECT
    t.Guid  AS ThreatGuid,
    c.Guid  AS ComponentGuid,
    sr.Guid AS SecurityRequirementGuid
FROM ComponentPropertyOptionThreatSecurityRequirementMapping m
INNER JOIN Threats t               ON m.ThreatId = t.Id
INNER JOIN Components c            ON m.ComponentId = c.Id
INNER JOIN SecurityRequirements sr ON m.SecurityRequirementId = sr.Id";
        }

        private async Task<IEnumerable<ComponentThreatSecurityRequirementMapping>> ExecuteMappingReaderAsync(SqlCommand command)
        {
            var mappings = new List<ComponentThreatSecurityRequirementMapping>();
            using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);

            int ordThreatGuid = reader.GetOrdinal("ThreatGuid");
            int ordComponentGuid = reader.GetOrdinal("ComponentGuid");
            int ordSecurityRequirementGuid = reader.GetOrdinal("SecurityRequirementGuid");

            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                mappings.Add(new ComponentThreatSecurityRequirementMapping
                {
                    ThreatGuid = reader.GetGuid(ordThreatGuid),
                    ComponentGuid = reader.GetGuid(ordComponentGuid),
                    SecurityRequirementGuid = reader.GetGuid(ordSecurityRequirementGuid),

                    // Not present in the unified mapping table schema you shared; keep model compatibility.
                    IsHidden = false,
                    IsOverridden = false
                });
            }

            return mappings;
        }
    }
}
