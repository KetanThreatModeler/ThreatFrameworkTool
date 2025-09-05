using Microsoft.Data.SqlClient;
using ThreatFramework.Core.Models.CoreEntities;
using ThreatFramework.Infra.Contract;
using ThreatFramework.Infrastructure.Interfaces.Repositories;

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
                    Id = (int)reader["Id"],
                    RiskId = (int)reader["RiskId"],
                    LibraryId = await _libraryCacheService.GetGuidByIdAsync((int)reader["LibraryId"]),
                    IsCompensatingControl = (bool)reader["IsCompensatingControl"],
                    IsHidden = (bool)reader["isHidden"],
                    IsOverridden = (bool)reader["IsOverridden"],
                    CreatedDate = (DateTime)reader["CreatedDate"],
                    LastUpdated = reader["LastUpdated"] as DateTime?,
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
    }
}
