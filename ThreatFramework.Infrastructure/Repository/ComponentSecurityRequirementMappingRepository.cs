using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using ThreatFramework.Core.ComponentMapping;
using ThreatFramework.Infra.Contract;
using ThreatFramework.Infra.Contract.Repository;

namespace ThreatFramework.Infrastructure.Repository
{
    public class ComponentSecurityRequirementMappingRepository : IComponentSecurityRequirementMappingRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;
        private readonly ILibraryCacheService _libraryCacheService;
        private readonly ILogger<ComponentSecurityRequirementMappingRepository> _logger;

        public ComponentSecurityRequirementMappingRepository(
            ISqlConnectionFactory connectionFactory, 
            ILibraryCacheService libraryCacheService,
            ILogger<ComponentSecurityRequirementMappingRepository> logger)
        {
            _connectionFactory = connectionFactory;
            _libraryCacheService = libraryCacheService;
            _logger = logger;
            
            _logger.LogInformation("ComponentSecurityRequirementMappingRepository initialized");
        }

        public async Task<IEnumerable<ComponentSecurityRequirementMapping>> GetMappingsByLibraryIdAsync(IEnumerable<Guid> libraryGuids)
        {
            _logger.LogInformation("Getting component security requirement mappings for {LibraryCount} library GUIDs", libraryGuids.Count());

            try
            {
                var libraryIds = await _libraryCacheService.GetIdsFromGuid(libraryGuids);
                _logger.LogInformation("Converted {GuidCount} library GUIDs to {IdCount} library IDs", 
                    libraryGuids.Count(), libraryIds.Count);

                if (!libraryIds.Any())
                {
                    _logger.LogWarning("No library IDs found for provided GUIDs, returning empty result");
                    return Enumerable.Empty<ComponentSecurityRequirementMapping>();
                }

                var libraryIdList = libraryIds.ToList();
                var libraryParameters = string.Join(",", libraryIdList.Select((_, i) => $"@lib{i}"));

                var baseQuery = BuildMappingSelectQuery();
                var sql = $@"{baseQuery} 
                            WHERE (sr.LibraryId IN ({libraryParameters}) OR c.LibraryId IN ({libraryParameters}))";

                _logger.LogInformation("Executing SQL query for component security requirement mappings");
                _logger.LogDebug("Base query: {BaseQuery}", baseQuery);
                _logger.LogDebug("Full SQL query: {SqlQuery}", sql);
                _logger.LogDebug("Library IDs: [{LibraryIds}]", string.Join(", ", libraryIdList));

                using var connection = await _connectionFactory.CreateOpenConnectionAsync();
                _logger.LogInformation("Database connection opened successfully");
                
                using var command = new SqlCommand(sql, connection);

                for (int i = 0; i < libraryIdList.Count; i++)
                {
                    command.Parameters.AddWithValue($"@lib{i}", libraryIdList[i]);
                }

                _logger.LogInformation("SQL parameters set, executing query...");
                var result = await ExecuteMappingReaderAsync(command);
                _logger.LogInformation("Retrieved {MappingCount} component security requirement mappings for {LibraryCount} libraries", 
                    result.Count(), libraryIdList.Count);

                return result;
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "SQL error occurred while getting component security requirement mappings. Error Number: {ErrorNumber}, Message: {Message}", 
                    sqlEx.Number, sqlEx.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while getting component security requirement mappings for library GUIDs");
                throw;
            }
        }

        public async Task<IEnumerable<ComponentSecurityRequirementMapping>> GetReadOnlyMappingsAsync()
        {
            _logger.LogInformation("Getting read-only component security requirement mappings");

            try
            {
                var readonlyLibraryIds = await _libraryCacheService.GetReadOnlyLibraryIdAsync();
                _logger.LogInformation("Found {ReadOnlyLibraryCount} read-only library IDs", readonlyLibraryIds.Count);

                if (!readonlyLibraryIds.Any())
                {
                    _logger.LogWarning("No read-only library IDs found, returning empty result");
                    return Enumerable.Empty<ComponentSecurityRequirementMapping>();
                }

                var libraryIdList = readonlyLibraryIds.ToList();
                var libraryParameters = string.Join(",", libraryIdList.Select((_, i) => $"@lib{i}"));

                var baseQuery = BuildMappingSelectQuery();
                var sql = $@"{baseQuery} 
                            WHERE (sr.LibraryId IN ({libraryParameters}) OR c.LibraryId IN ({libraryParameters}))";

                _logger.LogInformation("Executing SQL query for read-only component security requirement mappings");
                _logger.LogDebug("Base query: {BaseQuery}", baseQuery);
                _logger.LogDebug("Full SQL query: {SqlQuery}", sql);
                _logger.LogDebug("Library IDs: [{LibraryIds}]", string.Join(", ", libraryIdList));

                using var connection = await _connectionFactory.CreateOpenConnectionAsync();
                _logger.LogInformation("Database connection opened successfully");
                
                using var command = new SqlCommand(sql, connection);

                for (int i = 0; i < libraryIdList.Count; i++)
                {
                    command.Parameters.AddWithValue($"@lib{i}", libraryIdList[i]);
                }

                _logger.LogInformation("SQL parameters set, executing query...");
                var result = await ExecuteMappingReaderAsync(command);
                _logger.LogInformation("Retrieved {MappingCount} read-only component security requirement mappings for {LibraryCount} libraries", 
                    result.Count(), libraryIdList.Count);

                return result;
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "SQL error occurred while getting read-only component security requirement mappings. Error Number: {ErrorNumber}, Message: {Message}", 
                    sqlEx.Number, sqlEx.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while getting read-only component security requirement mappings");
                throw;
            }
        }

        private string BuildMappingSelectQuery()
        {
            var query = @"SELECT csrm.isHidden, csrm.IsOverridden, sr.Guid as SecurityRequirementGuid, c.Guid as ComponentGuid
                        FROM ComponentSecurityRequirementMapping csrm
                        INNER JOIN SecurityRequirements sr ON csrm.SecurityRequirementId = sr.Id
                        INNER JOIN Components c ON csrm.ComponentId = c.Id";
            
            _logger.LogDebug("Built base mapping select query: {Query}", query);
            return query;
        }

        private async Task<IEnumerable<ComponentSecurityRequirementMapping>> ExecuteMappingReaderAsync(SqlCommand command)
        {
            try
            {
                _logger.LogDebug("Starting to execute SQL command and read results");
                var mappings = new List<ComponentSecurityRequirementMapping>();
                using var reader = await command.ExecuteReaderAsync();
                _logger.LogDebug("SQL command executed successfully, reading data...");

                int recordCount = 0;
                while (await reader.ReadAsync())
                {
                    recordCount++;
                    var mapping = new ComponentSecurityRequirementMapping
                    {
                        SecurityRequirementGuid = (Guid)reader["SecurityRequirementGuid"],
                        ComponentGuid = (Guid)reader["ComponentGuid"],
                        IsHidden = (bool)reader["isHidden"],
                        IsOverridden = (bool)reader["IsOverridden"]
                    };
                    mappings.Add(mapping);
                    
                    if (recordCount == 1)
                    {
                        _logger.LogDebug("First record read successfully - ComponentGuid: {ComponentGuid}, SecurityRequirementGuid: {SecurityRequirementGuid}", 
                            mapping.ComponentGuid, mapping.SecurityRequirementGuid);
                    }
                }

                _logger.LogDebug("Finished reading {RecordCount} records from database", recordCount);
                return mappings;
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "SQL error occurred while executing mapping reader. Error Number: {ErrorNumber}, Message: {Message}, SQL State: {SqlState}", 
                    sqlEx.Number, sqlEx.Message, sqlEx.State);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while executing mapping reader");
                throw;
            }
        }
    }
}
