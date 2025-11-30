using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging; // Required for Logging
using ThreatFramework.Core.CoreEntities;
using ThreatFramework.Infra.Contract;
using ThreatFramework.Infra.Contract.Repository;
using ThreatModeler.TF.Infra.Implmentation.Helper; // Namespace for your Extension Methods

namespace ThreatFramework.Infrastructure.Repository
{
    public class ComponentRepository : IComponentRepository
    {
        private readonly ILibraryCacheService _libraryCacheService;
        private readonly ISqlConnectionFactory _connectionFactory;
        private readonly ILogger<ComponentRepository> _logger;

        public ComponentRepository(
            ILibraryCacheService libraryCacheService,
            ISqlConnectionFactory sqlConnectionFactory,
            ILogger<ComponentRepository> logger)
        {
            _libraryCacheService = libraryCacheService;
            _connectionFactory = sqlConnectionFactory;
            _logger = logger;
        }

        public async Task<IEnumerable<Component>> GetReadOnlyComponentsAsync()
        {
            _logger.LogInformation("Starting execution of GetReadOnlyComponentsAsync.");

            try
            {
                var readonlyLibraryIds = await _libraryCacheService.GetReadOnlyLibraryIdAsync();

                if (!readonlyLibraryIds.Any())
                {
                    _logger.LogInformation("No read-only libraries found. Returning empty list.");
                    return Enumerable.Empty<Component>();
                }

                var libraryIdList = readonlyLibraryIds.ToList();
                var libraryParameters = string.Join(",", libraryIdList.Select((_, i) => $"@lib{i}"));

                var sql = $@"{BuildComponentSelectQuery()} 
                            WHERE c.LibraryId IN ({libraryParameters})";

                using var connection = await _connectionFactory.CreateOpenConnectionAsync();
                using var command = new SqlCommand(sql, connection);

                for (int i = 0; i < libraryIdList.Count; i++)
                {
                    command.Parameters.AddWithValue($"@lib{i}", libraryIdList[i]);
                }

                var result = await ExecuteComponentReaderAsync(command);
                _logger.LogInformation("Successfully retrieved {Count} read-only components.", result.Count());

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in GetReadOnlyComponentsAsync.");
                throw;
            }
        }

        public async Task<IEnumerable<Component>> GetComponentsByLibraryIdAsync(IEnumerable<Guid> libraryIds)
        {
            _logger.LogInformation("Starting GetComponentsByLibraryIdAsync for {Count} libraries.", libraryIds?.Count() ?? 0);

            try
            {
                var ids = await _libraryCacheService.GetIdsFromGuid(libraryIds);

                if (!ids.Any())
                {
                    _logger.LogWarning("No matching Library IDs found for the provided GUIDs.");
                    return Enumerable.Empty<Component>();
                }

                var libraryIdList = ids.ToList();
                var libraryParameters = string.Join(",", libraryIdList.Select((_, i) => $"@lib{i}"));

                var sql = $@"{BuildComponentSelectQuery()} 
                            WHERE c.LibraryId IN ({libraryParameters})";

                using var connection = await _connectionFactory.CreateOpenConnectionAsync();
                using var command = new SqlCommand(sql, connection);

                for (int i = 0; i < libraryIdList.Count; i++)
                {
                    command.Parameters.AddWithValue($"@lib{i}", libraryIdList[i]);
                }

                var result = await ExecuteComponentReaderAsync(command);
                _logger.LogInformation("Successfully retrieved {Count} components by Library IDs.", result.Count());

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in GetComponentsByLibraryIdAsync.");
                throw;
            }
        }

        public async Task<IEnumerable<Guid>> GetGuidsAsync()
        {
            try
            {
                var sql = "SELECT Guid FROM Components";

                using var connection = await _connectionFactory.CreateOpenConnectionAsync();
                using var command = new SqlCommand(sql, connection);
                using var reader = await command.ExecuteReaderAsync();

                var guids = new List<Guid>();
                while (await reader.ReadAsync())
                {
                    guids.Add(reader.GetGuid(reader.GetOrdinal("Guid")));
                }
                return guids;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all Component GUIDs.");
                throw;
            }
        }

        public async Task<IEnumerable<(Guid ComponentGuid, Guid LibraryGuid)>> GetGuidsAndLibraryGuidsAsync()
        {
            try
            {
                const string sql = @"
                    SELECT c.Guid AS ComponentGuid, l.Guid AS LibraryGuid
                    FROM Components c
                    INNER JOIN Libraries l ON c.LibraryId = l.Id";

                using var connection = await _connectionFactory.CreateOpenConnectionAsync();
                using var command = new SqlCommand(sql, connection);

                var results = new List<(Guid, Guid)>();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var componentGuid = reader.GetGuid(reader.GetOrdinal("ComponentGuid"));
                    var libraryGuid = reader.GetGuid(reader.GetOrdinal("LibraryGuid"));

                    results.Add((componentGuid, libraryGuid));
                }

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Component/Library GUID pairs.");
                throw;
            }
        }

        public async Task<IEnumerable<Guid>> GetGuidsByLibraryIds(IEnumerable<Guid> libraryIds)
        {
            try
            {
                if (libraryIds == null || !libraryIds.Any())
                    return Enumerable.Empty<Guid>();

                var ids = await _libraryCacheService.GetIdsFromGuid(libraryIds);

                if (!ids.Any())
                    return Enumerable.Empty<Guid>();

                var libraryIdList = ids.ToList();
                var libraryParameters = string.Join(",", libraryIdList.Select((_, i) => $"@lib{i}"));

                var sql = $@"SELECT Guid 
                             FROM Components 
                             WHERE LibraryId IN ({libraryParameters})";

                using var connection = await _connectionFactory.CreateOpenConnectionAsync();
                using var command = new SqlCommand(sql, connection);

                for (int i = 0; i < libraryIdList.Count; i++)
                {
                    command.Parameters.AddWithValue($"@lib{i}", libraryIdList[i]);
                }

                var guids = new List<Guid>();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    guids.Add(reader.GetGuid(reader.GetOrdinal("Guid")));
                }

                return guids;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Component GUIDs by Library IDs.");
                throw;
            }
        }

        // --- Private Helpers ---

        private static string BuildComponentSelectQuery()
        {
            return @"SELECT c.Id, c.Guid, c.LibraryId, c.ComponentTypeId, ct.Guid AS ComponentTypeGuid, c.isHidden, c.IsOverriden, 
                            c.CreatedDate, c.LastUpdated, c.Name, c.ImagePath, c.Labels, 
                            c.Version, c.Description, c.ChineseDescription 
                    FROM Components c
                    INNER JOIN ComponentTypes ct ON c.ComponentTypeId = ct.Id";
        }

        private async Task<IEnumerable<Component>> ExecuteComponentReaderAsync(SqlCommand command)
        {
            var components = new List<Component>();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                components.Add(new Component
                {
                    // Value Types
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    Guid = reader.GetGuid(reader.GetOrdinal("Guid")),
                    LibraryGuid = await _libraryCacheService.GetGuidByIdAsync(reader.GetInt32(reader.GetOrdinal("LibraryId"))),
                    ComponentTypeGuid = reader.GetGuid(reader.GetOrdinal("ComponentTypeGuid")),

                    IsHidden = reader.GetBoolean(reader.GetOrdinal("isHidden")),
                    // Note: SQL Column is 'IsOverriden' (one d), Entity is 'IsOverridden' (two d's)
                    IsOverridden = reader.GetBoolean(reader.GetOrdinal("IsOverriden")),

                    CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                    LastUpdated = reader["LastUpdated"] == DBNull.Value ? null : (DateTime?)reader["LastUpdated"],

                    // Strings (Using DbValueExtensions for safety)
                    Name = reader["Name"].ToSafeString(),
                    ImagePath = reader["ImagePath"].ToSafeString(),
                    Version = reader["Version"].ToSafeString(),
                    Description = reader["Description"].ToSafeString(),
                    ChineseDescription = reader["ChineseDescription"].ToSafeString(),

                    // List (Using DbValueExtensions)
                    Labels = reader["Labels"].ToLabelList()
                });
            }

            return components;
        }
    }
}