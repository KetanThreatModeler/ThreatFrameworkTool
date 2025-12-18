using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using ThreatFramework.Infra.Contract;
using ThreatModeler.TF.Core.Model.ThreatMapping;
using ThreatModeler.TF.Infra.Contract.Repository.ThreatMapping;

namespace ThreatModeler.TF.Infra.Implmentation.Repository.ThreatMapping
{
    public class ThreatSecurityRequirementMappingRepository : IThreatSecurityRequirementMappingRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;
        private readonly ILibraryCacheService _libraryCacheService;

        public ThreatSecurityRequirementMappingRepository(
            ISqlConnectionFactory connectionFactory,
            ILibraryCacheService libraryCacheService)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _libraryCacheService = libraryCacheService ?? throw new ArgumentNullException(nameof(libraryCacheService));
        }

        public async Task<IEnumerable<ThreatSecurityRequirementMapping>> GetMappingsByLibraryIdAsync(IEnumerable<Guid> libraryGuids)
        {
            if (libraryGuids == null)
                throw new ArgumentNullException(nameof(libraryGuids));

            var libraryIds = await _libraryCacheService.GetIdsFromGuid(libraryGuids).ConfigureAwait(false);

            if (libraryIds == null || !libraryIds.Any())
                return Enumerable.Empty<ThreatSecurityRequirementMapping>();

            return await GetByLibraryIntIdsAsync(libraryIds).ConfigureAwait(false);
        }

        public async Task<IEnumerable<ThreatSecurityRequirementMapping>> GetReadOnlyMappingsAsync()
        {
            var readonlyLibraryIds = await _libraryCacheService.GetReadOnlyLibraryIdAsync().ConfigureAwait(false);

            if (readonlyLibraryIds == null || !readonlyLibraryIds.Any())
                return Enumerable.Empty<ThreatSecurityRequirementMapping>();

            return await GetByLibraryIntIdsAsync(readonlyLibraryIds).ConfigureAwait(false);
        }

        // --------------------------
        // Private helpers
        // --------------------------

        private async Task<IEnumerable<ThreatSecurityRequirementMapping>> GetByLibraryIntIdsAsync(IEnumerable<int> libraryIds)
        {
            var libraryIdList = libraryIds as IList<int> ?? libraryIds.ToList();

            if (libraryIdList.Count == 0)
                return Enumerable.Empty<ThreatSecurityRequirementMapping>();

            var libraryParameters = string.Join(",", libraryIdList.Select((_, i) => $"@lib{i}"));

            var sql = $@"
{BuildMappingSelectQuery()}
WHERE (t.LibraryId IN ({libraryParameters}) OR sr.LibraryId IN ({libraryParameters}));";

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
            // Table schema (per your SSMS script) only has:
            // SecurityRequirementId, ThreatId
            // No isHidden / IsOverridden columns.
            return @"
SELECT
    t.Guid  AS ThreatGuid,
    sr.Guid AS SecurityRequirementGuid
FROM ThreatSecurityRequirementMapping tsrm
INNER JOIN Threats t               ON tsrm.ThreatId = t.Id
INNER JOIN SecurityRequirements sr ON tsrm.SecurityRequirementId = sr.Id";
        }

        private async Task<IEnumerable<ThreatSecurityRequirementMapping>> ExecuteMappingReaderAsync(SqlCommand command)
        {
            var mappings = new List<ThreatSecurityRequirementMapping>();

            using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);

            int ordThreatGuid = reader.GetOrdinal("ThreatGuid");
            int ordSecurityRequirementGuid = reader.GetOrdinal("SecurityRequirementGuid");

            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                mappings.Add(new ThreatSecurityRequirementMapping
                {
                    ThreatGuid = reader.GetGuid(ordThreatGuid),
                    SecurityRequirementGuid = reader.GetGuid(ordSecurityRequirementGuid),

                    // Not present in table schema — keep model compatibility
                    IsHidden = false,
                    IsOverridden = false
                });
            }

            return mappings;
        }
    }
}
