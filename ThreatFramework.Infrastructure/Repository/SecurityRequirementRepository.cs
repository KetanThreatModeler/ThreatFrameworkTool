using Microsoft.Data.SqlClient;
using ThreatFramework.Core.CoreEntities;
using ThreatFramework.Infra.Contract;
using ThreatFramework.Infra.Contract.Repository;

namespace ThreatFramework.Infrastructure.Repository
{
    public class SecurityRequirementRepository : ISecurityRequirementRepository
    {
        private readonly ILibraryCacheService _libraryCacheService;
        private readonly ISqlConnectionFactory _connectionFactory;

        public SecurityRequirementRepository(ILibraryCacheService libraryCacheService, ISqlConnectionFactory sqlConnectionFactory)
        {
            _libraryCacheService = libraryCacheService;
            _connectionFactory = sqlConnectionFactory;
        }

        public async Task<IEnumerable<SecurityRequirement>> GetReadOnlySecurityRequirementsAsync()
        {
            var readonlyLibraryIds = await _libraryCacheService.GetReadOnlyLibraryIdAsync();

            if (!readonlyLibraryIds.Any())
                return Enumerable.Empty<SecurityRequirement>();

            var libraryIdList = readonlyLibraryIds.ToList();
            var libraryParameters = string.Join(",", libraryIdList.Select((_, i) => $"@lib{i}"));

            var sql = $@"{BuildSecurityRequirementSelectQuery()} 
                        WHERE LibraryId IN ({libraryParameters})";

            using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            using var command = new SqlCommand(sql, connection);

            for (int i = 0; i < libraryIdList.Count; i++)
            {
                command.Parameters.AddWithValue($"@lib{i}", libraryIdList[i]);
            }

            return await ExecuteSecurityRequirementReaderAsync(command);
        }

        private static string BuildSecurityRequirementSelectQuery()
        {
            return @"SELECT sr.Id, sr.RiskId, sr.LibraryId, sr.IsCompensatingControl, sr.isHidden, sr.IsOverridden, 
                            sr.CreatedDate, sr.LastUpdated, sr.Guid, sr.Name, sr.ChineseName, sr.Labels, 
                            sr.Description, sr.ChineseDescription 
                    FROM SecurityRequirements sr";
        }

        private async Task<IEnumerable<SecurityRequirement>> ExecuteSecurityRequirementReaderAsync(SqlCommand command)
        {
            var securityRequirements = new List<SecurityRequirement>();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                securityRequirements.Add(new SecurityRequirement
                {
                    RiskId = (int)reader["RiskId"],
                    LibraryId = await _libraryCacheService.GetGuidByIdAsync((int)reader["LibraryId"]),
                    IsCompensatingControl = (bool)reader["IsCompensatingControl"],
                    IsHidden = (bool)reader["isHidden"],
                    IsOverridden = (bool)reader["IsOverridden"],
                    Guid = (Guid)reader["Guid"],
                    Name = (string)reader["Name"],
                    ChineseName = reader["ChineseName"] as string,
                    Labels = reader["Labels"] as string,
                    Description = reader["Description"] as string,
                    ChineseDescription = reader["ChineseDescription"] as string
                });
            }

            return securityRequirements;
        }

        public async Task<IEnumerable<SecurityRequirement>> GetSecurityRequirementsByLibraryIdAsync(IEnumerable<Guid> libraryIds)
        {
            var ids = await _libraryCacheService.GetIdsFromGuid(libraryIds);

            if (!ids.Any())
                return Enumerable.Empty<SecurityRequirement>();

            var libraryIdList = ids.ToList();
            var libraryParameters = string.Join(",", libraryIdList.Select((_, i) => $"@lib{i}"));

            var sql = $@"{BuildSecurityRequirementSelectQuery()} 
                WHERE LibraryId IN ({libraryParameters})";

            using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            using var command = new SqlCommand(sql, connection);

            for (int i = 0; i < libraryIdList.Count; i++)
            {
                command.Parameters.AddWithValue($"@lib{i}", libraryIdList[i]);
            }

            return await ExecuteSecurityRequirementReaderAsync(command);
        }

        public async Task<IEnumerable<Guid>> GetGuidAsync()
        {
            var sql = "SELECT Guid FROM SecurityRequirements";

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

        public async Task<IEnumerable<(Guid SecurityRequirementGuid, Guid LibraryGuid)>> GetGuidsAndLibraryGuidsAsync()
        {
            const string sql = @"
        SELECT sr.Guid AS SecurityRequirementGuid, l.Guid AS LibraryGuid
        FROM SecurityRequirements sr
        INNER JOIN Libraries l ON sr.LibraryId = l.Id";

            using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            using var command = new SqlCommand(sql, connection);

            var results = new List<(Guid, Guid)>();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var srGuid = reader.GetGuid(reader.GetOrdinal("SecurityRequirementGuid"));
                var libraryGuid = reader.GetGuid(reader.GetOrdinal("LibraryGuid"));

                results.Add((srGuid, libraryGuid));
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
                 FROM SecurityRequirements
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

        public async Task<IEnumerable<(Guid SecurityRequirementGuid, Guid LibraryGuid)>> GetGuidsAndLibraryGuidsAsync(IEnumerable<Guid> libraryIds)
        {
            if (libraryIds == null || !libraryIds.Any())
                return Enumerable.Empty<(Guid SecurityRequirementGuid, Guid LibraryGuid)>();

            // Convert library GUIDs to their integer IDs used in the DB
            var ids = await _libraryCacheService.GetIdsFromGuid(libraryIds);

            if (!ids.Any())
                return Enumerable.Empty<(Guid SecurityRequirementGuid, Guid LibraryGuid)>();

            var libraryIdList = ids.ToList();
            var libraryParameters = string.Join(",", libraryIdList.Select((_, i) => $"@lib{i}"));

            var sql = $@"
        SELECT sr.Guid AS SecurityRequirementGuid, l.Guid AS LibraryGuid
        FROM SecurityRequirements sr
        INNER JOIN Libraries l ON sr.LibraryId = l.Id
        WHERE sr.LibraryId IN ({libraryParameters})";

            using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            using var command = new SqlCommand(sql, connection);

            for (int i = 0; i < libraryIdList.Count; i++)
            {
                command.Parameters.AddWithValue($"@lib{i}", libraryIdList[i]);
            }

            var results = new List<(Guid SecurityRequirementGuid, Guid LibraryGuid)>();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var srGuid = reader.GetGuid(reader.GetOrdinal("SecurityRequirementGuid"));
                var libraryGuid = reader.GetGuid(reader.GetOrdinal("LibraryGuid"));

                results.Add((srGuid, libraryGuid));
            }

            return results;
        }
    }
}
