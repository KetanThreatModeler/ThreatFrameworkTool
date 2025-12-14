using Microsoft.Data.SqlClient;
using ThreatFramework.Core.ComponentMapping;
using ThreatFramework.Infra.Contract;
using ThreatFramework.Infra.Contract.Repository;

namespace ThreatFramework.Infrastructure.Repository
{
    public class ComponentThreatSecurityRequirementMappingRepository : IComponentThreatSecurityRequirementMappingRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;
        private readonly ILibraryCacheService _libraryCacheService;

        public ComponentThreatSecurityRequirementMappingRepository(ISqlConnectionFactory connectionFactory, ILibraryCacheService libraryCacheService)
        {
            _connectionFactory = connectionFactory;
            _libraryCacheService = libraryCacheService;
        }

        public async Task<IEnumerable<ComponentThreatSecurityRequirementMapping>> GetMappingsByLibraryIdAsync(IEnumerable<Guid> libraryGuids)
        {
            var libraryIds = await _libraryCacheService.GetIdsFromGuid(libraryGuids);

            if (!libraryIds.Any())
                return Enumerable.Empty<ComponentThreatSecurityRequirementMapping>();

            var libraryIdList = libraryIds.ToList();
            var libraryParameters = string.Join(",", libraryIdList.Select((_, i) => $"@lib{i}"));

            var sql = $@"{BuildMappingSelectQuery()} 
                            WHERE (t.LibraryId IN ({libraryParameters}) OR c.LibraryId IN ({libraryParameters}))";

            using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            using var command = new SqlCommand(sql, connection);

            for (int i = 0; i < libraryIdList.Count; i++)
            {
                command.Parameters.AddWithValue($"@lib{i}", libraryIdList[i]);
            }

            return await ExecuteMappingReaderAsync(command);
        }

        public async Task<IEnumerable<ComponentThreatSecurityRequirementMapping>> GetReadOnlyMappingsAsync()
        {
            var readonlyLibraryIds = await _libraryCacheService.GetReadOnlyLibraryIdAsync();

            if (!readonlyLibraryIds.Any())
                return Enumerable.Empty<ComponentThreatSecurityRequirementMapping>();

            var libraryIdList = readonlyLibraryIds.ToList();
            var libraryParameters = string.Join(",", libraryIdList.Select((_, i) => $"@lib{i}"));

            var sql = $@"{BuildMappingSelectQuery()} 
                            WHERE (t.LibraryId IN ({libraryParameters}) OR c.LibraryId IN ({libraryParameters}))";

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
            return @"SELECT ctsrm.IsHidden, ctsrm.IsOverridden, t.Guid as ThreatGuid, c.Guid as ComponentGuid, sr.Guid as SecurityRequirementGuid
                        FROM ComponentThreatSecurityRequirementMapping ctsrm
                        INNER JOIN ComponentThreatMapping ctm ON ctsrm.ComponentThreatId = ctm.Id
                        INNER JOIN Threats t ON ctm.ThreatId = t.Id
                        INNER JOIN Components c ON ctm.ComponentId = c.Id
                        INNER JOIN SecurityRequirements sr ON ctsrm.SecurityRequirementId = sr.Id";
        }

        private async Task<IEnumerable<ComponentThreatSecurityRequirementMapping>> ExecuteMappingReaderAsync(SqlCommand command)
        {
            var mappings = new List<ComponentThreatSecurityRequirementMapping>();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                mappings.Add(new ComponentThreatSecurityRequirementMapping
                {
                    ThreatGuid = (Guid)reader["ThreatGuid"],
                    ComponentGuid = (Guid)reader["ComponentGuid"],
                    SecurityRequirementGuid = (Guid)reader["SecurityRequirementGuid"],
                    IsHidden = (bool)reader["IsHidden"],
                    IsOverridden = (bool)reader["IsOverridden"]
                });
            }

            return mappings;
        }
    }
}
