using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using ThreatFramework.Core.CoreEntities;
using ThreatFramework.Infra.Contract;
using ThreatFramework.Infra.Contract.Repository;

namespace ThreatFramework.Infrastructure.Repository
{
    public class ThreatRepository : IThreatRepository
    {
        private readonly ILibraryCacheService _libraryCacheService;
        private readonly ISqlConnectionFactory _connectionFactory;

        public ThreatRepository(ILibraryCacheService libraryCacheService, ISqlConnectionFactory sqlConnectionFactory)
        {
            _libraryCacheService = libraryCacheService;
            _connectionFactory = sqlConnectionFactory;
        }

        public async Task<IEnumerable<Threat>> GetReadOnlyThreatsAsync()
        {
            var readonlyLibraryIds = await _libraryCacheService.GetReadOnlyLibraryIdAsync();
            
            if (!readonlyLibraryIds.Any())
                return Enumerable.Empty<Threat>();

            var libraryIdList = readonlyLibraryIds.ToList();
            var libraryParameters = string.Join(",", libraryIdList.Select((_, i) => $"@lib{i}"));
            
            var sql = $@"{BuildThreatSelectQuery()} 
                        WHERE LibraryId IN ({libraryParameters})";

            using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            using var command = new SqlCommand(sql, connection);

            for (int i = 0; i < libraryIdList.Count; i++)
            {
                command.Parameters.AddWithValue($"@lib{i}", libraryIdList[i]);
            }

            return await ExecuteThreatReaderAsync(command);
        }

        private static string BuildThreatSelectQuery()
        {
            return @"SELECT t.Id, t.RiskId, t.LibraryId, t.Automated, t.isHidden, t.IsOverridden, t.CreatedDate, 
                            t.LastUpdated, t.Guid, t.Name, t.ChineseName, t.Labels, t.Description, t.Reference, 
                            t.Intelligence, t.ChineseDescription 
                    FROM Threats t";
        }

        private async Task<IEnumerable<Threat>> ExecuteThreatReaderAsync(SqlCommand command)
        {
            var threats = new List<Threat>();
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                threats.Add(new Threat
                {
                    Id = (int)reader["Id"],
                    RiskId = (int)reader["RiskId"],
                    LibraryGuid = await _libraryCacheService.GetGuidByIdAsync((int)reader["LibraryId"]),
                    Automated = (bool)reader["Automated"],
                    IsHidden = (bool)reader["isHidden"],
                    IsOverridden = (bool)reader["IsOverridden"],
                    CreatedDate = (DateTime)reader["CreatedDate"],
                    LastUpdated = reader["LastUpdated"] as DateTime?,
                    Guid = (Guid)reader["Guid"],
                    Name = (string)reader["Name"],
                    ChineseName = reader["ChineseName"] as string,
                    Labels = reader["Labels"] as string,
                    Description = reader["Description"] as string,
                    Reference = reader["Reference"] as string,
                    Intelligence = reader["Intelligence"] as string,
                    ChineseDescription = reader["ChineseDescription"] as string
                });
            }
            
            return threats;
        }

        public async Task<IEnumerable<Threat>> GetThreatsByLibraryIdAsync(IEnumerable<Guid> guids)
        {
            var ids = await _libraryCacheService.GetIdsFromGuid(guids);
    
            if (!ids.Any())
                return Enumerable.Empty<Threat>();

            var libraryIdList = ids.ToList();
            var libraryParameters = string.Join(",", libraryIdList.Select((_, i) => $"@lib{i}"));
            
            var sql = $@"{BuildThreatSelectQuery()} 
                        WHERE LibraryId IN ({libraryParameters})";

            using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            using var command = new SqlCommand(sql, connection);

            for (int i = 0; i < libraryIdList.Count; i++)
            {
                command.Parameters.AddWithValue($"@lib{i}", libraryIdList[i]);
            }

            return await ExecuteThreatReaderAsync(command);
        }

        public async Task<IEnumerable<Guid>> GetGuidsAsync()
        {
            var sql = "SELECT t.Guid FROM Threats t";

            using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            using var command = new SqlCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();

            var guids = new List<Guid>();
            while (await reader.ReadAsync())
            {
                guids.Add((Guid)reader["Guid"]);
            }

            return guids;
        }

        public async Task<IEnumerable<(Guid ThreatGuid, Guid LibraryGuid)>> GetGuidsAndLibraryGuidsAsync()
        {
            const string sql = @"
        SELECT t.Guid AS ThreatGuid, l.Guid AS LibraryGuid
        FROM Threats t
        INNER JOIN Libraries l ON t.LibraryId = l.Id";

            using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            using var command = new SqlCommand(sql, connection);

            var results = new List<(Guid, Guid)>();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var threatGuid = reader.GetGuid(reader.GetOrdinal("ThreatGuid"));
                var libraryGuid = reader.GetGuid(reader.GetOrdinal("LibraryGuid"));

                results.Add((threatGuid, libraryGuid));
            }

            return results;
        }

        public async Task<IEnumerable<Guid>> GetGuidsByLibraryIds(IEnumerable<Guid> libraryIds)
        {
            if (libraryIds == null || !libraryIds.Any())
                return Enumerable.Empty<Guid>();

            // Convert library GUIDs to their integer IDs used in the DB
            var ids = await _libraryCacheService.GetIdsFromGuid(libraryIds);

            if (!ids.Any())
                return Enumerable.Empty<Guid>();

            var libraryIdList = ids.ToList();
            var libraryParameters = string.Join(",", libraryIdList.Select((_, i) => $"@lib{i}"));

            var sql = $@"SELECT Guid
                 FROM Threats
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
    }
}
