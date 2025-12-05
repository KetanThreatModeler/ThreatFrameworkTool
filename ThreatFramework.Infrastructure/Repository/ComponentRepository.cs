using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging; // Required for Logging
using System.Data;
using ThreatFramework.Core.CoreEntities;
using ThreatFramework.Infra.Contract;
using ThreatFramework.Infra.Contract.Repository;
using ThreatModeler.TF.Infra.Implmentation.Helper;

namespace ThreatModeler.TF.Infra.Implmentation.Repository
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
                HashSet<int> readonlyLibraryIds = await _libraryCacheService.GetReadOnlyLibraryIdAsync();

                if (!readonlyLibraryIds.Any())
                {
                    _logger.LogInformation("No read-only libraries found. Returning empty list.");
                    return Enumerable.Empty<Component>();
                }

                List<int> libraryIdList = readonlyLibraryIds.ToList();
                string libraryParameters = string.Join(",", libraryIdList.Select((_, i) => $"@lib{i}"));

                string sql = $@"{BuildComponentSelectQuery()} 
                            WHERE c.LibraryId IN ({libraryParameters})";

                using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync();
                using SqlCommand command = new(sql, connection);

                for (int i = 0; i < libraryIdList.Count; i++)
                {
                    _ = command.Parameters.AddWithValue($"@lib{i}", libraryIdList[i]);
                }

                IEnumerable<Component> result = await ExecuteComponentReaderAsync(command);
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
                HashSet<int> ids = await _libraryCacheService.GetIdsFromGuid(libraryIds);

                if (!ids.Any())
                {
                    _logger.LogWarning("No matching Library IDs found for the provided GUIDs.");
                    return Enumerable.Empty<Component>();
                }

                List<int> libraryIdList = ids.ToList();
                string libraryParameters = string.Join(",", libraryIdList.Select((_, i) => $"@lib{i}"));

                string sql = $@"{BuildComponentSelectQuery()} 
                            WHERE c.LibraryId IN ({libraryParameters})";

                using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync();
                using SqlCommand command = new(sql, connection);

                for (int i = 0; i < libraryIdList.Count; i++)
                {
                    _ = command.Parameters.AddWithValue($"@lib{i}", libraryIdList[i]);
                }

                IEnumerable<Component> result = await ExecuteComponentReaderAsync(command);
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
                string sql = "SELECT Guid FROM Components";

                using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync();
                using SqlCommand command = new(sql, connection);
                using SqlDataReader reader = await command.ExecuteReaderAsync();

                List<Guid> guids = new();
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

                using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync();
                using SqlCommand command = new(sql, connection);

                List<(Guid, Guid)> results = new();
                using SqlDataReader reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    Guid componentGuid = reader.GetGuid(reader.GetOrdinal("ComponentGuid"));
                    Guid libraryGuid = reader.GetGuid(reader.GetOrdinal("LibraryGuid"));

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
                {
                    return Enumerable.Empty<Guid>();
                }

                HashSet<int> ids = await _libraryCacheService.GetIdsFromGuid(libraryIds);

                if (!ids.Any())
                {
                    return Enumerable.Empty<Guid>();
                }

                List<int> libraryIdList = ids.ToList();
                string libraryParameters = string.Join(",", libraryIdList.Select((_, i) => $"@lib{i}"));

                string sql = $@"SELECT Guid 
                             FROM Components 
                             WHERE LibraryId IN ({libraryParameters})";

                using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync();
                using SqlCommand command = new(sql, connection);

                for (int i = 0; i < libraryIdList.Count; i++)
                {
                    _ = command.Parameters.AddWithValue($"@lib{i}", libraryIdList[i]);
                }

                List<Guid> guids = new();
                using SqlDataReader reader = await command.ExecuteReaderAsync();

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
            List<Component> components = new();
            using SqlDataReader reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                components.Add(new Component
                {
                    // Value Types
                    Guid = reader.GetGuid(reader.GetOrdinal("Guid")),
                    LibraryGuid = await _libraryCacheService.GetGuidByIdAsync(reader.GetInt32(reader.GetOrdinal("LibraryId"))),
                    ComponentTypeGuid = reader.GetGuid(reader.GetOrdinal("ComponentTypeGuid")),

                    IsHidden = reader.GetBoolean(reader.GetOrdinal("isHidden")),
                    // Note: SQL Column is 'IsOverriden' (one d), Entity is 'IsOverridden' (two d's)
                    IsOverridden = reader.GetBoolean(reader.GetOrdinal("IsOverriden")),


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

        public async Task<IEnumerable<(Guid ComponentGuid, Guid LibraryGuid)>> GetGuidsAndLibraryGuidsAsync(IEnumerable<Guid> libraryIds)
        {
            _logger.LogInformation("Starting GetGuidsAndLibraryGuidsAsync for {Count} libraries.", libraryIds?.Count() ?? 0);

            try
            {
                if (libraryIds == null || !libraryIds.Any())
                {
                    _logger.LogInformation("No library GUIDs provided. Returning empty result.");
                    return Enumerable.Empty<(Guid ComponentGuid, Guid LibraryGuid)>();
                }

                // Convert library GUIDs to internal integer IDs using the cache service
                HashSet<int> ids = await _libraryCacheService.GetIdsFromGuid(libraryIds);

                if (!ids.Any())
                {
                    _logger.LogWarning("No matching Library IDs found for the provided GUIDs.");
                    return Enumerable.Empty<(Guid ComponentGuid, Guid LibraryGuid)>();
                }

                List<int> libraryIdList = ids.ToList();
                string libraryParameters = string.Join(",", libraryIdList.Select((_, i) => $"@lib{i}"));

                string sql = $@"
            SELECT c.Guid AS ComponentGuid, l.Guid AS LibraryGuid
            FROM Components c
            INNER JOIN Libraries l ON c.LibraryId = l.Id
            WHERE c.LibraryId IN ({libraryParameters})";

                using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync();
                using SqlCommand command = new(sql, connection);

                for (int i = 0; i < libraryIdList.Count; i++)
                {
                    _ = command.Parameters.AddWithValue($"@lib{i}", libraryIdList[i]);
                }

                List<(Guid ComponentGuid, Guid LibraryGuid)> results = new();
                using SqlDataReader reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    Guid componentGuid = reader.GetGuid(reader.GetOrdinal("ComponentGuid"));
                    Guid libraryGuid = reader.GetGuid(reader.GetOrdinal("LibraryGuid"));

                    results.Add((componentGuid, libraryGuid));
                }

                _logger.LogInformation("Successfully retrieved {Count} Component/Library GUID pairs.", results.Count);
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Component/Library GUID pairs for specified libraries.");
                throw;
            }
        }
    }
}