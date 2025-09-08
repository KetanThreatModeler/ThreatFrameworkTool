using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using ThreatFramework.Core.PropertyMapping;
using ThreatFramework.Infra.Contract;
using ThreatFramework.Infra.Contract.Repository;

namespace ThreatFramework.Infrastructure.Repository
{
    public class ComponentPropertyMappingRepository : IComponentPropertyMappingRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;
        private readonly ILibraryCacheService _libraryCacheService;
        private readonly ILogger<ComponentPropertyMappingRepository> _logger;

        public ComponentPropertyMappingRepository(
            ISqlConnectionFactory connectionFactory, 
            ILibraryCacheService libraryCacheService,
            ILogger<ComponentPropertyMappingRepository> logger)
        {
            _connectionFactory = connectionFactory;
            _libraryCacheService = libraryCacheService;
            _logger = logger;
            
            _logger.LogInformation("ComponentPropertyMappingRepository initialized");
        }

        public async Task<IEnumerable<ComponentPropertyMapping>> GetMappingsByLibraryIdAsync(IEnumerable<Guid> libraryGuids)
        {
            _logger.LogInformation("Getting component property mappings for {LibraryCount} library GUIDs", libraryGuids.Count());

            try
            {
                var libraryIds = await _libraryCacheService.GetIdsFromGuid(libraryGuids);
                _logger.LogInformation("Converted {GuidCount} library GUIDs to {IdCount} library IDs", 
                    libraryGuids.Count(), libraryIds.Count);

                if (!libraryIds.Any())
                {
                    _logger.LogWarning("No library IDs found for provided GUIDs, returning empty result");
                    return Enumerable.Empty<ComponentPropertyMapping>();
                }

                var libraryIdList = libraryIds.ToList();
                var libraryParameters = string.Join(",", libraryIdList.Select((_, i) => $"@lib{i}"));

                var baseQuery = BuildMappingSelectQuery();
                var sql = $@"{baseQuery} 
                            WHERE (p.LibraryId IN ({libraryParameters}) OR c.LibraryId IN ({libraryParameters}))";

                _logger.LogInformation("Executing SQL query for component property mappings");
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
                _logger.LogInformation("Retrieved {MappingCount} component property mappings for {LibraryCount} libraries", 
                    result.Count(), libraryIdList.Count);

                return result;
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "SQL error occurred while getting component property mappings. Error Number: {ErrorNumber}, Message: {Message}", 
                    sqlEx.Number, sqlEx.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while getting component property mappings for library GUIDs");
                throw;
            }
        }

        public async Task<IEnumerable<ComponentPropertyMapping>> GetReadOnlyMappingsAsync()
        {
            _logger.LogInformation("Getting read-only component property mappings");

            try
            {
                var readonlyLibraryIds = await _libraryCacheService.GetReadOnlyLibraryIdAsync();
                _logger.LogInformation("Found {ReadOnlyLibraryCount} read-only library IDs", readonlyLibraryIds.Count);

                if (!readonlyLibraryIds.Any())
                {
                    _logger.LogWarning("No read-only library IDs found, returning empty result");
                    return Enumerable.Empty<ComponentPropertyMapping>();
                }

                var libraryIdList = readonlyLibraryIds.ToList();
                var libraryParameters = string.Join(",", libraryIdList.Select((_, i) => $"@lib{i}"));

                var baseQuery = BuildMappingSelectQuery();
                var sql = $@"{baseQuery} 
                            WHERE (p.LibraryId IN ({libraryParameters}) OR c.LibraryId IN ({libraryParameters}))";

                _logger.LogInformation("Executing SQL query for read-only component property mappings");
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
                _logger.LogInformation("Retrieved {MappingCount} read-only component property mappings for {LibraryCount} libraries", 
                    result.Count(), libraryIdList.Count);

                return result;
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "SQL error occurred while getting read-only component property mappings. Error Number: {ErrorNumber}, Message: {Message}", 
                    sqlEx.Number, sqlEx.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while getting read-only component property mappings");
                throw;
            }
        }

        private string BuildMappingSelectQuery()
        {
            var query = @"SELECT cpm.Id, cpm.IsOptional, cpm.isHidden, cpm.IsOverridden, p.Guid as PropertyGuid, c.Guid as ComponentGuid
                    FROM ComponentPropertyMapping cpm
                    INNER JOIN Properties p ON cpm.PropertyId = p.Id
                    INNER JOIN Components c ON cpm.ComponentId = c.Id";
            
            _logger.LogDebug("Built base mapping select query: {Query}", query);
            return query;
        }

        private async Task<IEnumerable<ComponentPropertyMapping>> ExecuteMappingReaderAsync(SqlCommand command)
        {
            try
            {
                _logger.LogDebug("Starting to execute SQL command and read results");
                var mappings = new List<ComponentPropertyMapping>();
                using var reader = await command.ExecuteReaderAsync();
                _logger.LogDebug("SQL command executed successfully, reading data...");

                int recordCount = 0;
                while (await reader.ReadAsync())
                {
                    recordCount++;
                    var mapping = new ComponentPropertyMapping
                    {
                        Id = (int)reader["Id"],
                        PropertyGuid = (Guid)reader["PropertyGuid"],
                        ComponentGuid = (Guid)reader["ComponentGuid"],
                        IsOptional = (bool)reader["IsOptional"],
                        IsHidden = (bool)reader["isHidden"],
                        IsOverridden = (bool)reader["IsOverridden"]
                    };
                    mappings.Add(mapping);
                    
                    if (recordCount == 1)
                    {
                        _logger.LogDebug("First record read successfully - Id: {Id}, ComponentGuid: {ComponentGuid}, PropertyGuid: {PropertyGuid}", 
                            mapping.Id, mapping.ComponentGuid, mapping.PropertyGuid);
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
