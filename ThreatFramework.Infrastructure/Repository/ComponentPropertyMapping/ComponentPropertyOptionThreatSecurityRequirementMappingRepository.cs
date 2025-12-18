using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using ThreatFramework.Infra.Contract;
using ThreatFramework.Infra.Contract.Repository;
using ThreatModeler.TF.Core.Model.PropertyMapping;

namespace ThreatFramework.Infrastructure.Repository
{
    public class ComponentPropertyOptionThreatSecurityRequirementMappingRepository
        : IComponentPropertyOptionThreatSecurityRequirementMappingRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;
        private readonly ILibraryCacheService _libraryCacheService;

        public ComponentPropertyOptionThreatSecurityRequirementMappingRepository(
            ISqlConnectionFactory connectionFactory,
            ILibraryCacheService libraryCacheService)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _libraryCacheService = libraryCacheService ?? throw new ArgumentNullException(nameof(libraryCacheService));
        }

        public async Task<IEnumerable<ComponentPropertyOptionThreatSecurityRequirementMapping>> GetMappingsByLibraryGuidAsync(
            IEnumerable<Guid> libraryGuids)
        {
            if (libraryGuids == null)
                throw new ArgumentNullException(nameof(libraryGuids));

            var libraryIds = await _libraryCacheService.GetIdsFromGuid(libraryGuids).ConfigureAwait(false);

            if (libraryIds == null || !libraryIds.Any())
                return Enumerable.Empty<ComponentPropertyOptionThreatSecurityRequirementMapping>();

            return await GetByLibraryIntIdsAsync(libraryIds).ConfigureAwait(false);
        }

        public async Task<IEnumerable<ComponentPropertyOptionThreatSecurityRequirementMapping>> GetReadOnlyMappingsAsync()
        {
            var readonlyLibraryIds = await _libraryCacheService.GetReadOnlyLibraryIdAsync().ConfigureAwait(false);

            if (readonlyLibraryIds == null || !readonlyLibraryIds.Any())
                return Enumerable.Empty<ComponentPropertyOptionThreatSecurityRequirementMapping>();

            return await GetByLibraryIntIdsAsync(readonlyLibraryIds).ConfigureAwait(false);
        }

        // --------------------------
        // Private helpers
        // --------------------------

        private async Task<IEnumerable<ComponentPropertyOptionThreatSecurityRequirementMapping>> GetByLibraryIntIdsAsync(
            IEnumerable<int> libraryIds)
        {
            var libraryIdList = libraryIds as IList<int> ?? libraryIds.ToList();

            if (libraryIdList.Count == 0)
                return Enumerable.Empty<ComponentPropertyOptionThreatSecurityRequirementMapping>();

            var libraryParameters = string.Join(",", libraryIdList.Select((_, i) => $"@lib{i}"));

            // Requirement: ComponentId, PropertyId, PropertyOptionId, ThreatId, SecurityRequirementId must NOT be NULL.
            var sql = $@"
{BuildMappingSelectQuery()}
WHERE
    (p.LibraryId IN ({libraryParameters})
     OR c.LibraryId IN ({libraryParameters})
     OR t.LibraryId IN ({libraryParameters})
     OR sr.LibraryId IN ({libraryParameters}))
    AND m.ComponentId IS NOT NULL
    AND m.PropertyId IS NOT NULL
    AND m.PropertyOptionId IS NOT NULL
    AND m.ThreatId IS NOT NULL
    AND m.SecurityRequirementId IS NOT NULL;";

            using var connection = await _connectionFactory.CreateOpenConnectionAsync().ConfigureAwait(false);
            using var command = new SqlCommand(sql, connection);

            for (int i = 0; i < libraryIdList.Count; i++)
            {
                var p = command.Parameters.Add($"@lib{i}", System.Data.SqlDbType.Int);
                p.Value = libraryIdList[i];
            }

            return await ExecuteMappingReaderAsync(command).ConfigureAwait(false);
        }

        private static string BuildMappingSelectQuery()
        {
            // Unified mapping table only:
            // ComponentPropertyOptionThreatSecurityRequirementMapping
            return @"
SELECT
    c.Guid  AS ComponentGuid,
    p.Guid  AS PropertyGuid,
    po.Guid AS PropertyOptionGuid,
    t.Guid  AS ThreatGuid,
    sr.Guid AS SecurityRequirementGuid
FROM ComponentPropertyOptionThreatSecurityRequirementMapping m
INNER JOIN Components           c  ON m.ComponentId = c.Id
INNER JOIN Properties           p  ON m.PropertyId = p.Id
INNER JOIN PropertyOptions      po ON m.PropertyOptionId = po.Id
INNER JOIN Threats              t  ON m.ThreatId = t.Id
INNER JOIN SecurityRequirements sr ON m.SecurityRequirementId = sr.Id";
        }

        private async Task<IEnumerable<ComponentPropertyOptionThreatSecurityRequirementMapping>> ExecuteMappingReaderAsync(
            SqlCommand command)
        {
            var mappings = new List<ComponentPropertyOptionThreatSecurityRequirementMapping>();

            using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);

            int ordComponentGuid = reader.GetOrdinal("ComponentGuid");
            int ordPropertyGuid = reader.GetOrdinal("PropertyGuid");
            int ordPropertyOptionGuid = reader.GetOrdinal("PropertyOptionGuid");
            int ordThreatGuid = reader.GetOrdinal("ThreatGuid");
            int ordSecurityRequirementGuid = reader.GetOrdinal("SecurityRequirementGuid");

            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                mappings.Add(new ComponentPropertyOptionThreatSecurityRequirementMapping
                {
                    ComponentGuid = reader.GetGuid(ordComponentGuid),
                    PropertyGuid = reader.GetGuid(ordPropertyGuid),
                    PropertyOptionGuid = reader.GetGuid(ordPropertyOptionGuid),
                    ThreatGuid = reader.GetGuid(ordThreatGuid),
                    SecurityRequirementGuid = reader.GetGuid(ordSecurityRequirementGuid),

                    // Not present in unified mapping table schema — keep model compatibility
                    IsHidden = false,
                    IsOverridden = false
                });
            }

            return mappings;
        }
    }
}
