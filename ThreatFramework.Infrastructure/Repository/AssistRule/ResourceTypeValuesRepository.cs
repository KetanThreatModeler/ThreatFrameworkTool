using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThreatFramework.Infra.Contract;
using ThreatModeler.TF.Core.Model.AssistRules;
using ThreatModeler.TF.Infra.Contract.Repository.AssistRules;

namespace ThreatModeler.TF.Infra.Implmentation.Repository.AssistRule
{
    public class ResourceTypeValuesRepository : IResourceTypeValuesRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;
        private readonly ILibraryCacheService _libraryCacheService;
        private readonly ILogger<ResourceTypeValuesRepository> _logger;

        public ResourceTypeValuesRepository(
            ISqlConnectionFactory connectionFactory,
            ILibraryCacheService libraryCacheService,
            ILogger<ResourceTypeValuesRepository> logger)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _libraryCacheService = libraryCacheService ?? throw new ArgumentNullException(nameof(libraryCacheService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<ResourceTypeValues>> GetAllAsync()
        {
            try
            {
                _logger.LogDebug("Executing GetAllAsync for ResourceTypeValues.");

                using var connection = await _connectionFactory.CreateOpenConnectionAsync();
                using var command = new SqlCommand(BuildSelectQuery(), connection);

                return await ExecuteEntityReaderAsync(command);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching all ResourceTypeValues.");
                throw new Exception("Something went wrong while retrieving resource type values.", ex);
            }
        }

        public async Task<IEnumerable<ResourceTypeValues>> GetByLibraryIdAsync(Guid libraryId)
        {
            try
            {
                _logger.LogDebug(
                    "Executing GetByLibraryIdAsync for ResourceTypeValues. LibraryGuid: {LibraryGuid}",
                    libraryId);

                var dbLibraryId = await GetSingleLibraryDbIdAsync(libraryId);
                if (dbLibraryId == null)
                    return Enumerable.Empty<ResourceTypeValues>();

                var sql = $"{BuildSelectQuery()} WHERE LibraryId = @libraryId";

                using var connection = await _connectionFactory.CreateOpenConnectionAsync();
                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@libraryId", dbLibraryId.Value);

                return await ExecuteEntityReaderAsync(command);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error occurred while fetching ResourceTypeValues by LibraryGuid {LibraryGuid}",
                    libraryId);

                throw new Exception("Something went wrong while retrieving resource type values by library.", ex);
            }
        }

        public async Task<IEnumerable<ResourceTypeValues>> GetByLibraryIdsAsync(List<Guid> libraryIds)
        {
            try
            {
                _logger.LogDebug(
                    "Executing GetByLibraryIdsAsync for ResourceTypeValues. Count: {Count}",
                    libraryIds?.Count ?? 0);

                if (libraryIds == null || !libraryIds.Any())
                    return Enumerable.Empty<ResourceTypeValues>();

                var dbIds = await _libraryCacheService.GetIdsFromGuid(libraryIds);
                if (dbIds == null || !dbIds.Any())
                    return Enumerable.Empty<ResourceTypeValues>();

                var idList = dbIds.ToList();
                var parameters = string.Join(",", idList.Select((_, i) => $"@lib{i}"));

                var sql = $"{BuildSelectQuery()} WHERE LibraryId IN ({parameters})";

                using var connection = await _connectionFactory.CreateOpenConnectionAsync();
                using var command = new SqlCommand(sql, connection);

                for (int i = 0; i < idList.Count; i++)
                {
                    command.Parameters.AddWithValue($"@lib{i}", idList[i]);
                }

                return await ExecuteEntityReaderAsync(command);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching ResourceTypeValues by multiple LibraryGuids.");
                throw new Exception("Something went wrong while retrieving resource type values by libraries.", ex);
            }
        }

        public async Task<ResourceTypeValues> GetByResourceTypeValueAsync(string resourceTypeValue)
        {
            try
            {
                _logger.LogDebug(
                    "Executing GetByResourceTypeValueAsync. ResourceTypeValue: {ResourceTypeValue}",
                    resourceTypeValue);

                var sql = $"{BuildSelectQuery()} WHERE ResourceTypeValue = @resourceTypeValue";

                using var connection = await _connectionFactory.CreateOpenConnectionAsync();
                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@resourceTypeValue", resourceTypeValue);

                var results = await ExecuteEntityReaderAsync(command);
                return results.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error occurred while fetching ResourceTypeValues by ResourceTypeValue {ResourceTypeValue}",
                    resourceTypeValue);

                throw new Exception("Something went wrong while retrieving resource type values by resource type value.", ex);
            }
        }

        public async Task<IEnumerable<string>> GetResourceTypeValueAsync()
        {
            const string sql = @"SELECT ResourceTypeValue FROM [dbo].[ResourceTypeValues]";

            try
            {
                _logger.LogDebug("Executing GetResourceTypeValueAsync (all ResourceTypeValue strings).");

                using var connection = await _connectionFactory.CreateOpenConnectionAsync();
                using var command = new SqlCommand(sql, connection);

                return await ExecuteStringReaderAsync(command, "ResourceTypeValue");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching all ResourceTypeValue strings.");
                throw new Exception("Something went wrong while retrieving resource type value strings.", ex);
            }
        }

        public async Task<IEnumerable<string>> GetResourceTypeValueByLibraryIdAsync(List<Guid> libGuids)
        {
            try
            {
                _logger.LogDebug(
                    "Executing GetResourceTypeValueByLibraryIdAsync. LibraryGuids Count: {Count}",
                    libGuids?.Count ?? 0);

                if (libGuids == null || !libGuids.Any())
                    return Enumerable.Empty<string>();

                var dbIds = await _libraryCacheService.GetIdsFromGuid(libGuids);
                if (dbIds == null || !dbIds.Any())
                    return Enumerable.Empty<string>();

                var idList = dbIds.ToList();
                var parameters = string.Join(",", idList.Select((_, i) => $"@lib{i}"));

                var sql = $@"
                    SELECT ResourceTypeValue
                    FROM [dbo].[ResourceTypeValues]
                    WHERE LibraryId IN ({parameters})";

                using var connection = await _connectionFactory.CreateOpenConnectionAsync();
                using var command = new SqlCommand(sql, connection);

                for (int i = 0; i < idList.Count; i++)
                {
                    command.Parameters.AddWithValue($"@lib{i}", idList[i]);
                }

                return await ExecuteStringReaderAsync(command, "ResourceTypeValue");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching ResourceTypeValue strings by LibraryGuids.");
                throw new Exception("Something went wrong while retrieving resource type values by libraries.", ex);
            }
        }

        private static string BuildSelectQuery()
        {
            return @"
                SELECT
                    ResourceName,
                    ResourceTypeValue,
                    ComponentGuid,
                    LibraryId
                FROM [dbo].[ResourceTypeValues]";
        }

        private async Task<IEnumerable<ResourceTypeValues>> ExecuteEntityReaderAsync(SqlCommand command)
        {
            var results = new List<ResourceTypeValues>();

            using var reader = await command.ExecuteReaderAsync();

            var resourceNameOrdinal = reader.GetOrdinal("ResourceName");
            var resourceTypeValueOrdinal = reader.GetOrdinal("ResourceTypeValue");
            var componentGuidOrdinal = reader.GetOrdinal("ComponentGuid");
            var libraryIdOrdinal = reader.GetOrdinal("LibraryId");

            while (await reader.ReadAsync())
            {
                var dbLibraryId = reader.GetInt32(libraryIdOrdinal);

                results.Add(new ResourceTypeValues
                {
                    ResourceName = reader.IsDBNull(resourceNameOrdinal) ? null : reader.GetString(resourceNameOrdinal),
                    ResourceTypeValue = reader.IsDBNull(resourceTypeValueOrdinal) ? null : reader.GetString(resourceTypeValueOrdinal),
                    ComponentGuid = reader.GetGuid(componentGuidOrdinal),
                    LibraryId = await _libraryCacheService.GetGuidByIdAsync(dbLibraryId)
                });
            }

            return results;
        }

        private static async Task<IEnumerable<string>> ExecuteStringReaderAsync(SqlCommand command, string columnName)
        {
            var results = new List<string>();

            using var reader = await command.ExecuteReaderAsync();
            var ordinal = reader.GetOrdinal(columnName);

            while (await reader.ReadAsync())
            {
                results.Add(reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal));
            }

            return results;
        }

        private async Task<int?> GetSingleLibraryDbIdAsync(Guid libraryGuid)
        {
            // ILibraryCacheService only supports GetIdsFromGuid(IEnumerable<Guid>),
            // so for a single GUID we call it with a single-item list.
            var ids = await _libraryCacheService.GetIdsFromGuid(new[] { libraryGuid });
            if (ids == null || ids.Count == 0)
                return null;

            // Should be exactly one, but we take First safely.
            return ids.First();
        }
    }
}
