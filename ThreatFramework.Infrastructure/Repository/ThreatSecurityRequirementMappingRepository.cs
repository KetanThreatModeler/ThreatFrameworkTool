using Microsoft.Data.SqlClient;
using ThreatFramework.Infra.Contract;
using ThreatFramework.Infra.Contract.Repository;
using ThreatModeler.TF.Core.Model.ThreatMapping;

namespace ThreatFramework.Infrastructure.Repository
{
    public class ThreatSecurityRequirementMappingRepository : IThreatSecurityRequirementMappingRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;
        private readonly ILibraryCacheService _libraryCacheService;

        public ThreatSecurityRequirementMappingRepository(ISqlConnectionFactory connectionFactory, ILibraryCacheService libraryCacheService)
        {
            _connectionFactory = connectionFactory;
            _libraryCacheService = libraryCacheService;
        }

        public async Task<IEnumerable<ThreatSecurityRequirementMapping>> GetMappingsByLibraryIdAsync(IEnumerable<Guid> libraryGuids)
        {
            var libraryIds = await _libraryCacheService.GetIdsFromGuid(libraryGuids);

            if (!libraryIds.Any())
                return Enumerable.Empty<ThreatSecurityRequirementMapping>();

            var libraryIdList = libraryIds.ToList();
            var libraryParameters = string.Join(",", libraryIdList.Select((_, i) => $"@lib{i}"));

            var sql = $@"{BuildMappingSelectQuery()} 
                        WHERE (t.LibraryId IN ({libraryParameters}) OR sr.LibraryId IN ({libraryParameters}))";

            using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            using var command = new SqlCommand(sql, connection);

            for (int i = 0; i < libraryIdList.Count; i++)
            {
                command.Parameters.AddWithValue($"@lib{i}", libraryIdList[i]);
            }

            return await ExecuteMappingReaderAsync(command);
        }

        public async Task<IEnumerable<ThreatSecurityRequirementMapping>> GetReadOnlyMappingsAsync()
        {
            var readonlyLibraryIds = await _libraryCacheService.GetReadOnlyLibraryIdAsync();

            if (!readonlyLibraryIds.Any())
                return Enumerable.Empty<ThreatSecurityRequirementMapping>();

            var libraryIdList = readonlyLibraryIds.ToList();
            var libraryParameters = string.Join(",", libraryIdList.Select((_, i) => $"@lib{i}"));

            var sql = $@"{BuildMappingSelectQuery()} 
                        WHERE (t.LibraryId IN ({libraryParameters}) OR sr.LibraryId IN ({libraryParameters}))";

            using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            using var command = new SqlCommand(sql, connection);

            for (int i = 0; i < libraryIdList.Count; i++)
            {
                command.Parameters.AddWithValue($"@lib{i}", libraryIdList[i]);
            }

            return await ExecuteMappingReaderAsync(command);
        }

        private static string BuildMappingSelectQuery()
        {
            return @"SELECT tsrm.isHidden, tsrm.IsOverridden, t.Guid as ThreatGuid, sr.Guid as SecurityRequirementGuid
                    FROM ThreatSecurityRequirementMapping tsrm
                    INNER JOIN Threats t ON tsrm.ThreatId = t.Id
                    INNER JOIN SecurityRequirements sr ON tsrm.SecurityRequirementId = sr.Id";
        }

        private async Task<IEnumerable<ThreatSecurityRequirementMapping>> ExecuteMappingReaderAsync(SqlCommand command)
        {
            var mappings = new List<ThreatSecurityRequirementMapping>();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                mappings.Add(new ThreatSecurityRequirementMapping
                {
                    ThreatGuid = (Guid)reader["ThreatGuid"],
                    SecurityRequirementGuid = (Guid)reader["SecurityRequirementGuid"],
                    IsHidden = (bool)reader["isHidden"],
                    IsOverridden = (bool)reader["IsOverridden"]
                });
            }

            return mappings;
        }
    }
}
