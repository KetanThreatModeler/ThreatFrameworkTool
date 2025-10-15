using Microsoft.Data.SqlClient;
using ThreatFramework.Core.PropertyMapping;
using ThreatFramework.Infra.Contract;
using ThreatFramework.Infra.Contract.Repository;
namespace ThreatFramework.Infrastructure.Repository
{
    public class ComponentPropertyOptionThreatSecurityRequirementMappingRepository : IComponentPropertyOptionThreatSecurityRequirementMappingRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;
        private readonly ILibraryCacheService _libraryCacheService;

        public ComponentPropertyOptionThreatSecurityRequirementMappingRepository(ISqlConnectionFactory connectionFactory, ILibraryCacheService libraryCacheService)
        {
            _connectionFactory = connectionFactory;
            _libraryCacheService = libraryCacheService;
        }

        public async Task<IEnumerable<ComponentPropertyOptionThreatSecurityRequirementMapping>> GetMappingsByLibraryGuidAsync(IEnumerable<Guid> libraryGuids)
        {
            var libraryIds = await _libraryCacheService.GetIdsFromGuid(libraryGuids);

            if (!libraryIds.Any())
                return Enumerable.Empty<ComponentPropertyOptionThreatSecurityRequirementMapping>();

            var libraryIdList = libraryIds.ToList();
            var libraryParameters = string.Join(",", libraryIdList.Select((_, i) => $"@lib{i}"));

            var sql = $@"{BuildMappingSelectQuery()} 
                            WHERE (p.LibraryId IN ({libraryParameters}) OR c.LibraryId IN ({libraryParameters}) OR t.LibraryId IN ({libraryParameters}) OR sr.LibraryId IN ({libraryParameters}))";

            using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            using var command = new SqlCommand(sql, connection);

            for (int i = 0; i < libraryIdList.Count; i++)
            {
                command.Parameters.AddWithValue($"@lib{i}", libraryIdList[i]);
            }

            return await ExecuteMappingReaderAsync(command);
        }

        public async Task<IEnumerable<ComponentPropertyOptionThreatSecurityRequirementMapping>> GetReadOnlyMappingsAsync()
        {
            var readonlyLibraryIds = await _libraryCacheService.GetReadOnlyLibraryIdAsync();

            if (!readonlyLibraryIds.Any())
                return Enumerable.Empty<ComponentPropertyOptionThreatSecurityRequirementMapping>();

            var libraryIdList = readonlyLibraryIds.ToList();
            var libraryParameters = string.Join(",", libraryIdList.Select((_, i) => $"@lib{i}"));

            var sql = $@"{BuildMappingSelectQuery()} 
                            WHERE (p.LibraryId IN ({libraryParameters}) OR c.LibraryId IN ({libraryParameters}) OR t.LibraryId IN ({libraryParameters}) OR sr.LibraryId IN ({libraryParameters}))";

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
            return @"SELECT cpotsrm.Id, cpotsrm.isHidden, cpotsrm.IsOverridden, 
                            c.Guid as ComponentGuid, p.Guid as PropertyGuid, po.Guid as PropertyOptionGuid, 
                            t.Guid as ThreatGuid, sr.Guid as SecurityRequirementGuid
                        FROM ComponentPropertyOptionThreatSecurityRequirementMapping cpotsrm
                        INNER JOIN ComponentPropertyOptionThreatMapping cpotm ON cpotsrm.ComponentPropertyOptionThreatId = cpotm.Id
                        INNER JOIN ComponentPropertyOptionMapping cpom ON cpotm.ComponentPropertyOptionId = cpom.Id
                        INNER JOIN ComponentPropertyMapping cp ON cpom.ComponentPropertyId = cp.Id
                        INNER JOIN Components c ON cp.ComponentId = c.Id
                        INNER JOIN Properties p ON cp.PropertyId = p.Id
                        INNER JOIN PropertyOptions po ON cpom.PropertyOptionId = po.Id
                        INNER JOIN Threats t ON cpotm.ThreatId = t.Id
                        INNER JOIN SecurityRequirements sr ON cpotsrm.SecurityRequirementId = sr.Id";
        }

        private async Task<IEnumerable<ComponentPropertyOptionThreatSecurityRequirementMapping>> ExecuteMappingReaderAsync(SqlCommand command)
        {
            var mappings = new List<ComponentPropertyOptionThreatSecurityRequirementMapping>();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                mappings.Add(new ComponentPropertyOptionThreatSecurityRequirementMapping
                {
                    Id = (int)reader["Id"],
                    ComponentGuid = (Guid)reader["ComponentGuid"],
                    PropertyGuid = (Guid)reader["PropertyGuid"],
                    PropertyOptionGuid = (Guid)reader["PropertyOptionGuid"],
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
