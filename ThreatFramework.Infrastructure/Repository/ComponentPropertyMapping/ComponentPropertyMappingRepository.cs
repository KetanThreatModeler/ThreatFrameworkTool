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
    public class ComponentPropertyMappingRepository : IComponentPropertyMappingRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;
        private readonly ILibraryCacheService _libraryCacheService;

        public ComponentPropertyMappingRepository(
            ISqlConnectionFactory connectionFactory,
            ILibraryCacheService libraryCacheService)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _libraryCacheService = libraryCacheService ?? throw new ArgumentNullException(nameof(libraryCacheService));
        }

        // --------------------------
        // Public API
        // --------------------------

        public async Task<IEnumerable<ComponentPropertyMapping>> GetMappingsByLibraryIdAsync(IEnumerable<Guid> libraryGuids)
        {
            if (libraryGuids == null)
                throw new ArgumentNullException(nameof(libraryGuids));

            var libraryIds = await _libraryCacheService.GetIdsFromGuid(libraryGuids).ConfigureAwait(false);

            if (libraryIds == null || !libraryIds.Any())
                return Enumerable.Empty<ComponentPropertyMapping>();

            return await GetByLibraryIntIdsAsync(libraryIds).ConfigureAwait(false);
        }

        public async Task<IEnumerable<ComponentPropertyMapping>> GetReadOnlyMappingsAsync()
        {
            var readonlyLibraryIds = await _libraryCacheService.GetReadOnlyLibraryIdAsync().ConfigureAwait(false);

            if (readonlyLibraryIds == null || !readonlyLibraryIds.Any())
                return Enumerable.Empty<ComponentPropertyMapping>();

            return await GetByLibraryIntIdsAsync(readonlyLibraryIds).ConfigureAwait(false);
        }

        // --------------------------
        // Private helpers
        // --------------------------

        private async Task<IEnumerable<ComponentPropertyMapping>> GetByLibraryIntIdsAsync(IEnumerable<int> libraryIds)
        {
            var libraryIdList = libraryIds as IList<int> ?? libraryIds.ToList();

            if (libraryIdList.Count == 0)
                return Enumerable.Empty<ComponentPropertyMapping>();

            var libraryParameters = string.Join(",", libraryIdList.Select((_, i) => $"@lib{i}"));

            var sql = $@"
{BuildMappingSelectQuery()}
WHERE
    c.LibraryId IN ({libraryParameters})
    AND m.ComponentId IS NOT NULL
    AND m.PropertyId IS NOT NULL;";

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
            return @"
SELECT
    p.Guid AS PropertyGuid,
    c.Guid AS ComponentGuid,
    m.IsOptional
FROM ComponentPropertyOptionThreatSecurityRequirementMapping m
INNER JOIN Properties  p ON m.PropertyId  = p.Id
INNER JOIN Components  c ON m.ComponentId = c.Id";
        }

        private async Task<IEnumerable<ComponentPropertyMapping>> ExecuteMappingReaderAsync(SqlCommand command)
        {
            var mappings = new List<ComponentPropertyMapping>();

            using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);

            int ordPropertyGuid = reader.GetOrdinal("PropertyGuid");
            int ordComponentGuid = reader.GetOrdinal("ComponentGuid");
            int ordIsOptional = reader.GetOrdinal("IsOptional");

            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                mappings.Add(new ComponentPropertyMapping
                {
                    PropertyGuid = reader.GetGuid(ordPropertyGuid),
                    ComponentGuid = reader.GetGuid(ordComponentGuid),
                    IsOptional = !reader.IsDBNull(ordIsOptional) && reader.GetBoolean(ordIsOptional),

                    // Not present in unified mapping table — keep model compatibility
                    IsHidden = false,
                    IsOverridden = false
                });
            }

            return mappings;
        }
    }
}
