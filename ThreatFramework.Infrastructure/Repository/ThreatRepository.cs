using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging; // Required for Logging
using System.Data;
using ThreatFramework.Core.CoreEntities;
using ThreatFramework.Infra.Contract;
using ThreatFramework.Infra.Contract.Repository;
using ThreatModeler.TF.Infra.Implmentation.Helper;

namespace ThreatModeler.TF.Infra.Implmentation.Repository
{
    public class ThreatRepository : IThreatRepository
    {
        private readonly ILibraryCacheService _libraryCacheService;
        private readonly ISqlConnectionFactory _connectionFactory;
        private readonly ILogger<ThreatRepository> _logger;

        public ThreatRepository(
            ILibraryCacheService libraryCacheService,
            ISqlConnectionFactory sqlConnectionFactory,
            ILogger<ThreatRepository> logger)
        {
            _libraryCacheService = libraryCacheService;
            _connectionFactory = sqlConnectionFactory;
            _logger = logger;
        }

        public async Task<IEnumerable<Threat>> GetReadOnlyThreatsAsync()
        {
            _logger.LogInformation("Starting execution of GetReadOnlyThreatsAsync.");

            try
            {
                HashSet<int> readonlyLibraryIds = await _libraryCacheService.GetReadOnlyLibraryIdAsync();

                if (!readonlyLibraryIds.Any())
                {
                    _logger.LogInformation("No read-only libraries found. Returning empty list.");
                    return Enumerable.Empty<Threat>();
                }

                List<int> libraryIdList = readonlyLibraryIds.ToList();
                string libraryParameters = string.Join(",", libraryIdList.Select((_, i) => $"@lib{i}"));

                string sql = $@"{BuildThreatSelectQuery()} 
                            WHERE LibraryId IN ({libraryParameters})";

                using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync();
                using SqlCommand command = new(sql, connection);

                // Add parameters safely
                for (int i = 0; i < libraryIdList.Count; i++)
                {
                    _ = command.Parameters.AddWithValue($"@lib{i}", libraryIdList[i]);
                }

                IEnumerable<Threat> result = await ExecuteThreatReaderAsync(command);
                _logger.LogInformation("Successfully retrieved {Count} read-only threats.", result.Count());

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in GetReadOnlyThreatsAsync.");
                throw;
            }
        }

        public async Task<IEnumerable<Threat>> GetThreatsByLibraryIdAsync(IEnumerable<Guid> guids)
        {
            _logger.LogInformation("Starting GetThreatsByLibraryIdAsync for {Count} libraries.", guids?.Count() ?? 0);

            try
            {
                HashSet<int> ids = await _libraryCacheService.GetIdsFromGuid(guids);

                if (!ids.Any())
                {
                    _logger.LogWarning("No matching Library IDs found for the provided GUIDs.");
                    return Enumerable.Empty<Threat>();
                }

                List<int> libraryIdList = ids.ToList();
                string libraryParameters = string.Join(",", libraryIdList.Select((_, i) => $"@lib{i}"));

                string sql = $@"{BuildThreatSelectQuery()} 
                            WHERE LibraryId IN ({libraryParameters})";

                using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync();
                using SqlCommand command = new(sql, connection);

                for (int i = 0; i < libraryIdList.Count; i++)
                {
                    _ = command.Parameters.AddWithValue($"@lib{i}", libraryIdList[i]);
                }

                IEnumerable<Threat> result = await ExecuteThreatReaderAsync(command);
                _logger.LogInformation("Successfully retrieved {Count} threats by Library ID.", result.Count());

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in GetThreatsByLibraryIdAsync.");
                throw;
            }
        }

        public async Task<IEnumerable<Guid>> GetGuidsAsync()
        {
            try
            {
                string sql = "SELECT t.Guid FROM Threats t";
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
                _logger.LogError(ex, "Error retrieving all Threat GUIDs.");
                throw;
            }
        }

        public async Task<IEnumerable<(Guid ThreatGuid, Guid LibraryGuid)>> GetGuidsAndLibraryGuidsAsync()
        {
            try
            {
                const string sql = @"
                    SELECT t.Guid AS ThreatGuid, l.Guid AS LibraryGuid
                    FROM Threats t
                    INNER JOIN Libraries l ON t.LibraryId = l.Id";

                using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync();
                using SqlCommand command = new(sql, connection);

                List<(Guid, Guid)> results = new();
                using SqlDataReader reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    Guid threatGuid = reader.GetGuid(reader.GetOrdinal("ThreatGuid"));
                    Guid libraryGuid = reader.GetGuid(reader.GetOrdinal("LibraryGuid"));
                    results.Add((threatGuid, libraryGuid));
                }

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Guid mapping pairs.");
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

                string sql = $@"SELECT Guid FROM Threats WHERE LibraryId IN ({libraryParameters})";

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
                _logger.LogError(ex, "Error retrieving Guids by Library Ids.");
                throw;
            }
        }

        // --- Private Helpers ---

        private static string BuildThreatSelectQuery()
        {
            return @"SELECT t.Id, t.RiskId, t.LibraryId, t.Automated, t.isHidden, t.IsOverridden, t.CreatedDate, 
                            t.LastUpdated, t.Guid, t.Name, t.ChineseName, t.Labels, t.Description, t.Reference, 
                            t.Intelligence, t.ChineseDescription 
                    FROM Threats t";
        }

        private async Task<IEnumerable<Threat>> ExecuteThreatReaderAsync(SqlCommand command)
        {
            List<Threat> threats = new();
            using SqlDataReader reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                // Note: GetOrdinal is more efficient and safer than string indexing
                threats.Add(new Threat
                {
                    // Value Types (Int, Guid, Bool, Date)
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    RiskId = reader.GetInt32(reader.GetOrdinal("RiskId")),
                    // Assuming Service handles mapping logic
                    LibraryGuid = await _libraryCacheService.GetGuidByIdAsync(reader.GetInt32(reader.GetOrdinal("LibraryId"))),

                    Automated = reader.GetBoolean(reader.GetOrdinal("Automated")),
                    IsHidden = reader.GetBoolean(reader.GetOrdinal("isHidden")),
                    IsOverridden = reader.GetBoolean(reader.GetOrdinal("IsOverridden")),
                    Guid = reader.GetGuid(reader.GetOrdinal("Guid")),

                    // CLEAN CODE: Using Extension methods for Strings
                    Name = reader["Name"].ToSafeString(),
                    ChineseName = reader["ChineseName"].ToSafeString(),
                    Description = reader["Description"].ToSafeString(),
                    Reference = reader["Reference"].ToSafeString(),
                    Intelligence = reader["Intelligence"].ToSafeString(),
                    ChineseDescription = reader["ChineseDescription"].ToSafeString(),

                    // CLEAN CODE: Using Extension method for List Parsing
                    Labels = reader["Labels"].ToLabelList()
                });
            }

            return threats;
        }

        public async Task<IEnumerable<(Guid ThreatGuid, Guid LibraryGuid)>> GetGuidsAndLibraryGuidsAsync(IEnumerable<Guid> libraryIds)
        {
            _logger.LogInformation("Starting GetGuidsAndLibraryGuidsAsync for {Count} libraries.", libraryIds?.Count() ?? 0);

            try
            {
                if (libraryIds == null || !libraryIds.Any())
                {
                    _logger.LogInformation("No library GUIDs provided. Returning empty result.");
                    return Enumerable.Empty<(Guid ThreatGuid, Guid LibraryGuid)>();
                }

                // Convert library GUIDs to integer IDs used in DB
                HashSet<int> ids = await _libraryCacheService.GetIdsFromGuid(libraryIds);

                if (!ids.Any())
                {
                    _logger.LogWarning("No matching Library IDs found for the provided GUIDs.");
                    return Enumerable.Empty<(Guid ThreatGuid, Guid LibraryGuid)>();
                }

                List<int> libraryIdList = ids.ToList();
                string libraryParameters = string.Join(",", libraryIdList.Select((_, i) => $"@lib{i}"));

                string sql = $@"
            SELECT t.Guid AS ThreatGuid, l.Guid AS LibraryGuid
            FROM Threats t
            INNER JOIN Libraries l ON t.LibraryId = l.Id
            WHERE t.LibraryId IN ({libraryParameters})";

                using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync();
                using SqlCommand command = new(sql, connection);

                for (int i = 0; i < libraryIdList.Count; i++)
                {
                    _ = command.Parameters.AddWithValue($"@lib{i}", libraryIdList[i]);
                }

                List<(Guid ThreatGuid, Guid LibraryGuid)> results = new();
                using SqlDataReader reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    Guid threatGuid = reader.GetGuid(reader.GetOrdinal("ThreatGuid"));
                    Guid libraryGuid = reader.GetGuid(reader.GetOrdinal("LibraryGuid"));

                    results.Add((threatGuid, libraryGuid));
                }

                _logger.LogInformation("Successfully retrieved {Count} Threat/Library GUID pairs.", results.Count);
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Threat/Library GUID pairs for specified libraries.");
                throw;
            }
        }
    }
}