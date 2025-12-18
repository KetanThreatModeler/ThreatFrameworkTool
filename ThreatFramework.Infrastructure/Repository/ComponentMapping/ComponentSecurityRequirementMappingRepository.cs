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
    public class ComponentSecurityRequirementMappingRepository : IComponentSecurityRequirementMappingRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;
        private readonly ILibraryCacheService _libraryCacheService;
        private readonly ILogger<ComponentSecurityRequirementMappingRepository> _logger;

        private const int DefaultCommandTimeoutSeconds = 30;

        public ComponentSecurityRequirementMappingRepository(
            ISqlConnectionFactory connectionFactory,
            ILibraryCacheService libraryCacheService,
            ILogger<ComponentSecurityRequirementMappingRepository> logger)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _libraryCacheService = libraryCacheService ?? throw new ArgumentNullException(nameof(libraryCacheService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<ComponentSecurityRequirementMapping>> GetMappingsByLibraryIdAsync(IEnumerable<Guid> libraryGuids)
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
                    return Enumerable.Empty<ComponentSecurityRequirementMapping>();
                }

                var libraryIds = await _libraryCacheService.GetIdsFromGuid(libraryGuidList).ConfigureAwait(false);

                if (libraryIds == null || libraryIds.Count == 0)
                {
                    _logger.LogInformation("{Method} - No matching SQL Library IDs found. Returning empty result.", methodName);
                    return Enumerable.Empty<ComponentSecurityRequirementMapping>();
                }

                return await GetByLibraryIntIdsAsync(libraryIds).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Method} - Error occurred while retrieving mappings by library GUIDs.", methodName);
                throw;
            }
        }

        public async Task<IEnumerable<ComponentSecurityRequirementMapping>> GetReadOnlyMappingsAsync()
        {
            const string methodName = nameof(GetReadOnlyMappingsAsync);

            try
            {
                _logger.LogInformation("{Method} - Starting.", methodName);

                var readonlyLibraryIds = await _libraryCacheService.GetReadOnlyLibraryIdAsync().ConfigureAwait(false);

                if (readonlyLibraryIds == null || readonlyLibraryIds.Count == 0)
                {
                    _logger.LogInformation("{Method} - No read-only library IDs found. Returning empty result.", methodName);
                    return Enumerable.Empty<ComponentSecurityRequirementMapping>();
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

        private async Task<IEnumerable<ComponentSecurityRequirementMapping>> GetByLibraryIntIdsAsync(IEnumerable<int> libraryIds)
        {
            const string methodName = nameof(GetByLibraryIntIdsAsync);

            var libraryIdList = libraryIds as IList<int> ?? libraryIds.ToList();

            if (libraryIdList.Count == 0)
                return Enumerable.Empty<ComponentSecurityRequirementMapping>();

            var libraryParameters = string.Join(",", libraryIdList.Select((_, i) => $"@lib{i}"));

            var baseQuery = BuildMappingSelectQuery();
            var sql = $@"
{baseQuery}
WHERE
    (sr.LibraryId IN ({libraryParameters}) OR c.LibraryId IN ({libraryParameters}))
    AND m.ComponentId IS NOT NULL
    AND m.SecurityRequirementId IS NOT NULL
    AND m.PropertyId IS NULL
    AND m.PropertyOptionId IS NULL
    AND m.ThreatId IS NULL;";

            _logger.LogDebug("{Method} - Executing SQL. LibraryIds: {LibraryIds}. SQL: {Sql}",
                methodName, string.Join(",", libraryIdList), sql);

            using var connection = await _connectionFactory.CreateOpenConnectionAsync().ConfigureAwait(false);
            using var command = CreateCommand(connection, sql);

            AddLibraryIdParameters(command, libraryIdList);

            return await ExecuteMappingReaderAsync(command).ConfigureAwait(false);
        }

        private static string BuildMappingSelectQuery()
        {
            return @"
SELECT
    sr.Guid AS SecurityRequirementGuid,
    c.Guid  AS ComponentGuid
FROM ComponentPropertyOptionThreatSecurityRequirementMapping m
INNER JOIN SecurityRequirements sr ON m.SecurityRequirementId = sr.Id
INNER JOIN Components c            ON m.ComponentId = c.Id";
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

        private async Task<IEnumerable<ComponentSecurityRequirementMapping>> ExecuteMappingReaderAsync(SqlCommand command)
        {
            const string methodName = nameof(ExecuteMappingReaderAsync);

            var mappings = new List<ComponentSecurityRequirementMapping>();

            try
            {
                using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);

                int ordSrGuid = reader.GetOrdinal("SecurityRequirementGuid");
                int ordCompGuid = reader.GetOrdinal("ComponentGuid");

                while (await reader.ReadAsync().ConfigureAwait(false))
                {
                    var mapping = new ComponentSecurityRequirementMapping
                    {
                        SecurityRequirementGuid = reader.GetGuid(ordSrGuid),
                        ComponentGuid = reader.GetGuid(ordCompGuid),

                        // These columns don't exist in the provided mapping table schema.
                        // Keep model compatibility by defaulting.
                        IsHidden = false,
                        IsOverridden = false
                    };

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
