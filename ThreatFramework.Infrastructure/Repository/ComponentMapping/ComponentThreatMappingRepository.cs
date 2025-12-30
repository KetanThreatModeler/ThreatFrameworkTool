using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using ThreatFramework.Infra.Contract;
using ThreatFramework.Infra.Contract.Repository;
using ThreatModeler.TF.Core.Model.ComponentMapping;

namespace ThreatFramework.Infrastructure.Repository
{
    public class ComponentThreatMappingRepository : IComponentThreatMappingRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;
        private readonly ILibraryCacheService _libraryCacheService;
        private readonly ILogger<ComponentThreatMappingRepository> _logger;

        private const int DefaultCommandTimeoutSeconds = 30;

        public ComponentThreatMappingRepository(
            ISqlConnectionFactory connectionFactory,
            ILibraryCacheService libraryCacheService,
            ILogger<ComponentThreatMappingRepository> logger)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _libraryCacheService = libraryCacheService ?? throw new ArgumentNullException(nameof(libraryCacheService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<ComponentThreatMapping>> GetMappingsByLibraryIdAsync(IEnumerable<Guid> libraryGuids)
        {
            const string methodName = nameof(GetMappingsByLibraryIdAsync);

            if (libraryGuids == null)
                throw new ArgumentNullException(nameof(libraryGuids));

            try
            {
                var libraryGuidList = libraryGuids as IList<Guid> ?? libraryGuids.ToList();
                _logger.LogInformation("{Method} - Starting for {Count} library GUIDs.", methodName, libraryGuidList.Count);

                if (libraryGuidList.Count == 0)
                {
                    _logger.LogInformation("{Method} - No library GUIDs provided. Returning empty result.", methodName);
                    return Enumerable.Empty<ComponentThreatMapping>();
                }

                var libraryIds = await _libraryCacheService.GetIdsFromGuid(libraryGuidList).ConfigureAwait(false);

                if (libraryIds == null || libraryIds.Count == 0)
                {
                    _logger.LogInformation("{Method} - No matching SQL Library IDs found. Returning empty result.", methodName);
                    return Enumerable.Empty<ComponentThreatMapping>();
                }

                return await GetByLibraryIntIdsAsync(libraryIds).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Method} - Error occurred while retrieving mappings by library GUIDs.", methodName);
                throw;
            }
        }

        public async Task<IEnumerable<ComponentThreatMapping>> GetReadOnlyMappingsAsync()
        {
            const string methodName = nameof(GetReadOnlyMappingsAsync);

            try
            {
                _logger.LogInformation("{Method} - Starting.", methodName);

                var readonlyLibraryIds = await _libraryCacheService.GetReadOnlyLibraryIdAsync().ConfigureAwait(false);

                if (readonlyLibraryIds == null || readonlyLibraryIds.Count == 0)
                {
                    _logger.LogInformation("{Method} - No read-only library IDs found. Returning empty result.", methodName);
                    return Enumerable.Empty<ComponentThreatMapping>();
                }

                return await GetByLibraryIntIdsAsync(readonlyLibraryIds).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Method} - Error occurred while retrieving read-only mappings.", methodName);
                throw;
            }
        }

        // --------------------------
        // Private helpers
        // --------------------------

        private async Task<IEnumerable<ComponentThreatMapping>> GetByLibraryIntIdsAsync(IEnumerable<int> libraryIds)
        {
            const string methodName = nameof(GetByLibraryIntIdsAsync);

            var libraryIdList = libraryIds as IList<int> ?? libraryIds.ToList();

            if (libraryIdList.Count == 0)
                return Enumerable.Empty<ComponentThreatMapping>();

            var libraryParameters = string.Join(",", libraryIdList.Select((_, i) => $"@lib{i}"));

            var baseQuery = BuildMappingSelectQuery();
            var sql = $@"
{baseQuery}
WHERE
    c.LibraryId IN ({libraryParameters})
    AND m.ComponentId IS NOT NULL
    AND m.ThreatId IS NOT NULL
    AND m.PropertyId IS NULL
    AND m.PropertyOptionId IS NULL;";

            _logger.LogDebug("{Method} - Executing SQL. LibraryIds: {LibraryIds}. SQL: {Sql}",
                methodName, string.Join(",", libraryIdList), sql);

            using var connection = await _connectionFactory.CreateOpenConnectionAsync().ConfigureAwait(false);
            using var command = CreateCommand(connection, sql);

            AddLibraryIdParameters(command, libraryIdList);

            return await ExecuteMappingReaderAsync(command).ConfigureAwait(false);
        }

        private static string BuildMappingSelectQuery()
        {
            // Uses the unified mapping table:
            // ComponentPropertyOptionThreatSecurityRequirementMapping
            return @"
SELECT
    m.IsDefault,
    m.IsOptional,
    m.ApplicableOnComponentId,
    t.Guid AS ThreatGuid,
    c.Guid AS ComponentGuid
FROM ComponentPropertyOptionThreatSecurityRequirementMapping m
INNER JOIN Threats t    ON m.ThreatId = t.Id
INNER JOIN Components c ON m.ComponentId = c.Id";
        }

        private SqlCommand CreateCommand(SqlConnection connection, string sql)
        {
            return new SqlCommand(sql, connection)
            {
                CommandType = System.Data.CommandType.Text,
                CommandTimeout = DefaultCommandTimeoutSeconds
            };
        }

        private static void AddLibraryIdParameters(SqlCommand command, IList<int> libraryIdList)
        {
            for (int i = 0; i < libraryIdList.Count; i++)
            {
                var p = command.Parameters.Add($"@lib{i}", System.Data.SqlDbType.Int);
                p.Value = libraryIdList[i];
            }
        }

        private async Task<IEnumerable<ComponentThreatMapping>> ExecuteMappingReaderAsync(SqlCommand command)
        {
            const string methodName = nameof(ExecuteMappingReaderAsync);

            var mappings = new List<ComponentThreatMapping>();

            try
            {
                using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);

                int ordThreatGuid = reader.GetOrdinal("ThreatGuid");
                int ordComponentGuid = reader.GetOrdinal("ComponentGuid");

                // Optional columns (exist in unified mapping table per your script)
                int ordIsDefault = reader.GetOrdinal("IsDefault");
                int ordIsOptional = reader.GetOrdinal("IsOptional");
                int ordApplicableOnComponentId = reader.GetOrdinal("ApplicableOnComponentId");

                while (await reader.ReadAsync().ConfigureAwait(false))
                {
                    var mapping = new ComponentThreatMapping
                    {
                        ThreatGuid = reader.GetGuid(ordThreatGuid),
                        ComponentGuid = reader.GetGuid(ordComponentGuid),

                        // These columns are NOT in the SSMS script for the unified mapping table.
                        // Keep model compatibility by defaulting.
                        IsHidden = false,
                        IsOverridden = false,

                        // Also not in that script; keep prior behavior stable.
                        UsedForMitigation = false
                    };

                    // If your domain model has these fields and you want to use them, you can map them here.
                    // (Leaving as no-ops by default avoids breaking changes.)
                    // Example if you later add them:
                    // mapping.IsDefault = !reader.IsDBNull(ordIsDefault) && reader.GetBoolean(ordIsDefault);
                    // mapping.IsOptional = !reader.IsDBNull(ordIsOptional) && reader.GetBoolean(ordIsOptional);
                    // mapping.ApplicableOnComponentId = reader.IsDBNull(ordApplicableOnComponentId) ? (int?)null : reader.GetInt32(ordApplicableOnComponentId);

                    mappings.Add(mapping);
                }

                _logger.LogInformation("{Method} - Retrieved {Count} mappings.", methodName, mappings.Count);
                return mappings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Method} - Failed to execute/mapping reader. SQL: {Sql}", methodName, command.CommandText);
                throw;
            }
        }
    }
}
