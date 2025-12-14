using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
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
            const string methodName = nameof(GetReadOnlyComponentsAsync);
            _logger.LogInformation("{Method} - Starting execution.", methodName);

            try
            {
                HashSet<int> readonlyLibraryIds = await _libraryCacheService.GetReadOnlyLibraryIdAsync();

                if (!readonlyLibraryIds.Any())
                {
                    _logger.LogInformation("{Method} - No read-only libraries found. Returning empty list.", methodName);
                    return Enumerable.Empty<Component>();
                }

                List<int> libraryIdList = readonlyLibraryIds.ToList();
                string libraryParameters = string.Join(",", libraryIdList.Select((_, i) => $"@lib{i}"));

                string sql = $@"{BuildComponentSelectQuery()} 
                                WHERE c.LibraryId IN ({libraryParameters})";

                _logger.LogDebug("{Method} - Executing SQL: {Sql} with LibraryIds: {LibraryIds}",
                    methodName, sql, string.Join(",", libraryIdList));

                using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync();
                using SqlCommand command = new(sql, connection);

                for (int i = 0; i < libraryIdList.Count; i++)
                {
                    _ = command.Parameters.AddWithValue($"@lib{i}", libraryIdList[i]);
                }

                IEnumerable<Component> result = await ExecuteComponentReaderAsync(command);
                int count = result.Count();
                _logger.LogInformation("{Method} - Successfully retrieved {Count} read-only components.", methodName, count);

                return result;
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "{Method} - SQL error occurred while retrieving read-only components.", methodName);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Method} - Unexpected error occurred.", methodName);
                throw;
            }
        }

        public async Task<IEnumerable<Component>> GetComponentsByLibraryIdAsync(IEnumerable<Guid> libraryIds)
        {
            const string methodName = nameof(GetComponentsByLibraryIdAsync);
            int inputCount = libraryIds?.Count() ?? 0;

            _logger.LogInformation("{Method} - Starting for {Count} library GUIDs.", methodName, inputCount);

            try
            {
                HashSet<int> ids = await _libraryCacheService.GetIdsFromGuid(libraryIds);

                if (!ids.Any())
                {
                    _logger.LogWarning("{Method} - No matching Library IDs found for the provided GUIDs.", methodName);
                    return Enumerable.Empty<Component>();
                }

                List<int> libraryIdList = ids.ToList();
                string libraryParameters = string.Join(",", libraryIdList.Select((_, i) => $"@lib{i}"));

                string sql = $@"{BuildComponentSelectQuery()} 
                                WHERE c.LibraryId IN ({libraryParameters})";

                _logger.LogDebug("{Method} - Executing SQL: {Sql} with LibraryIds: {LibraryIds}",
                    methodName, sql, string.Join(",", libraryIdList));

                using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync();
                using SqlCommand command = new(sql, connection);

                for (int i = 0; i < libraryIdList.Count; i++)
                {
                    _ = command.Parameters.AddWithValue($"@lib{i}", libraryIdList[i]);
                }

                IEnumerable<Component> result = await ExecuteComponentReaderAsync(command);
                int count = result.Count();

                _logger.LogInformation("{Method} - Successfully retrieved {Count} components by Library IDs.", methodName, count);

                return result;
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "{Method} - SQL error occurred while retrieving components by Library IDs.", methodName);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Method} - Unexpected error occurred.", methodName);
                throw;
            }
        }

        public async Task<IEnumerable<Guid>> GetGuidsAsync()
        {
            const string methodName = nameof(GetGuidsAsync);
            const string sql = "SELECT Guid FROM Components";

            _logger.LogInformation("{Method} - Starting execution.", methodName);

            try
            {
                using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync();
                using SqlCommand command = new(sql, connection);
                using SqlDataReader reader = await command.ExecuteReaderAsync();

                List<Guid> guids = new();

                while (await reader.ReadAsync())
                {
                    guids.Add(reader.GetGuid(reader.GetOrdinal("Guid")));
                }

                _logger.LogInformation("{Method} - Successfully retrieved {Count} component GUIDs.", methodName, guids.Count);
                return guids;
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "{Method} - SQL error occurred while retrieving component GUIDs. Query: {Sql}", methodName, sql);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Method} - Unexpected error occurred.", methodName);
                throw;
            }
        }

        public async Task<IEnumerable<(Guid ComponentGuid, Guid LibraryGuid)>> GetGuidsAndLibraryGuidsAsync()
        {
            const string methodName = "GetGuidsAndLibraryGuidsAsync_NoFilter";
            const string sql = @"
                    SELECT c.Guid AS ComponentGuid, l.Guid AS LibraryGuid
                    FROM Components c
                    INNER JOIN Libraries l ON c.LibraryId = l.Id";

            _logger.LogInformation("{Method} - Starting execution (no library filter).", methodName);

            try
            {
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

                _logger.LogInformation("{Method} - Successfully retrieved {Count} Component/Library GUID pairs.", methodName, results.Count);
                return results;
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "{Method} - SQL error occurred while retrieving Component/Library GUID pairs. Query: {Sql}", methodName, sql);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Method} - Unexpected error occurred.", methodName);
                throw;
            }
        }

        public async Task<IEnumerable<Guid>> GetGuidsByLibraryIds(IEnumerable<Guid> libraryIds)
        {
            const string methodName = nameof(GetGuidsByLibraryIds);

            _logger.LogInformation("{Method} - Starting for {Count} library GUIDs.", methodName, libraryIds?.Count() ?? 0);

            try
            {
                if (libraryIds == null || !libraryIds.Any())
                {
                    _logger.LogInformation("{Method} - No library GUIDs provided. Returning empty result.", methodName);
                    return Enumerable.Empty<Guid>();
                }

                HashSet<int> ids = await _libraryCacheService.GetIdsFromGuid(libraryIds);

                if (!ids.Any())
                {
                    _logger.LogWarning("{Method} - No matching Library IDs found for the provided GUIDs.", methodName);
                    return Enumerable.Empty<Guid>();
                }

                List<int> libraryIdList = ids.ToList();
                string libraryParameters = string.Join(",", libraryIdList.Select((_, i) => $"@lib{i}"));

                string sql = $@"SELECT Guid 
                                FROM Components 
                                WHERE LibraryId IN ({libraryParameters})";

                _logger.LogDebug("{Method} - Executing SQL: {Sql} with LibraryIds: {LibraryIds}",
                    methodName, sql, string.Join(",", libraryIdList));

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

                _logger.LogInformation("{Method} - Successfully retrieved {Count} Component GUIDs.", methodName, guids.Count);
                return guids;
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "{Method} - SQL error occurred while retrieving GUIDs by Library IDs.", methodName);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Method} - Unexpected error occurred.", methodName);
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
            const string methodName = nameof(ExecuteComponentReaderAsync);

            _logger.LogDebug("{Method} - Executing reader for command: {CommandText}", methodName, command.CommandText);

            List<Component> components = new();

            try
            {
                using SqlDataReader reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    try
                    {
                        var libraryId = reader.GetInt32(reader.GetOrdinal("LibraryId"));
                        Guid libraryGuid = await _libraryCacheService.GetGuidByIdAsync(libraryId);

                        components.Add(new Component
                        {
                            // Value Types
                            Guid = reader.GetGuid(reader.GetOrdinal("Guid")),
                            LibraryGuid = libraryGuid,
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
                    catch (Exception exRow)
                    {
                        _logger.LogError(exRow,
                            "{Method} - Error mapping Component row. Skipping this row.", methodName);
                        // You can choose to rethrow if you don't want partial results:
                        // throw;
                    }
                }

                _logger.LogDebug("{Method} - Successfully mapped {Count} components.", methodName, components.Count);
                return components;
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "{Method} - SQL error while executing component reader.", methodName);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Method} - Unexpected error while executing component reader.", methodName);
                throw;
            }
        }

        public async Task<IEnumerable<(Guid ComponentGuid, Guid LibraryGuid)>> GetGuidsAndLibraryGuidsAsync(IEnumerable<Guid> libraryIds)
        {
            const string methodName = nameof(GetGuidsAndLibraryGuidsAsync);

            _logger.LogInformation("{Method} - Starting for {Count} library GUIDs.", methodName, libraryIds?.Count() ?? 0);

            try
            {
                if (libraryIds == null || !libraryIds.Any())
                {
                    _logger.LogInformation("{Method} - No library GUIDs provided. Returning empty result.", methodName);
                    return Enumerable.Empty<(Guid ComponentGuid, Guid LibraryGuid)>();
                }

                HashSet<int> ids = await _libraryCacheService.GetIdsFromGuid(libraryIds);

                if (!ids.Any())
                {
                    _logger.LogWarning("{Method} - No matching Library IDs found for the provided GUIDs.", methodName);
                    return Enumerable.Empty<(Guid ComponentGuid, Guid LibraryGuid)>();
                }

                List<int> libraryIdList = ids.ToList();
                string libraryParameters = string.Join(",", libraryIdList.Select((_, i) => $"@lib{i}"));

                string sql = $@"
                    SELECT c.Guid AS ComponentGuid, l.Guid AS LibraryGuid
                    FROM Components c
                    INNER JOIN Libraries l ON c.LibraryId = l.Id
                    WHERE c.LibraryId IN ({libraryParameters})";

                _logger.LogDebug("{Method} - Executing SQL: {Sql} with LibraryIds: {LibraryIds}",
                    methodName, sql, string.Join(",", libraryIdList));

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

                _logger.LogInformation("{Method} - Successfully retrieved {Count} Component/Library GUID pairs.", methodName, results.Count);
                return results;
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "{Method} - SQL error occurred while retrieving Component/Library GUID pairs for specified libraries.", methodName);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Method} - Unexpected error occurred.", methodName);
                throw;
            }
        }
    }
}
