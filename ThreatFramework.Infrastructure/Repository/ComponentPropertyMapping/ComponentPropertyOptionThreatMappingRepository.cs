using Microsoft.Data.SqlClient;
using ThreatFramework.Core.PropertyMapping;
using ThreatFramework.Infra.Contract;
using ThreatFramework.Infra.Contract.Repository;

namespace ThreatFramework.Infrastructure.Repository
{
    public class ComponentPropertyOptionThreatMappingRepository : IComponentPropertyOptionThreatMappingRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;
        private readonly ILibraryCacheService _libraryCacheService;

        public ComponentPropertyOptionThreatMappingRepository(ISqlConnectionFactory connectionFactory, ILibraryCacheService libraryCacheService)
        {
            _connectionFactory = connectionFactory;
            _libraryCacheService = libraryCacheService;
        }

        public async Task<IEnumerable<ComponentPropertyOptionThreatMapping>> GetMappingsByLibraryGuidAsync(IEnumerable<Guid> libraryGuids)
        {
            var libraryIds = await _libraryCacheService.GetIdsFromGuid(libraryGuids);

            if (!libraryIds.Any())
                return Enumerable.Empty<ComponentPropertyOptionThreatMapping>();

            var libraryIdList = libraryIds.ToList();
            var libraryParameters = string.Join(",", libraryIdList.Select((_, i) => $"@lib{i}"));

            var sql = $@"{BuildMappingSelectQuery()} 
                            WHERE (p.LibraryId IN ({libraryParameters}) OR c.LibraryId IN ({libraryParameters}) OR t.LibraryId IN ({libraryParameters}))";

            using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            using var command = new SqlCommand(sql, connection);

            for (int i = 0; i < libraryIdList.Count; i++)
            {
                command.Parameters.AddWithValue($"@lib{i}", libraryIdList[i]);
            }

            return await ExecuteMappingReaderAsync(command);
        }

        public async Task<IEnumerable<ComponentPropertyOptionThreatMapping>> GetReadOnlyMappingsAsync()
        {
            var readonlyLibraryIds = await _libraryCacheService.GetReadOnlyLibraryIdAsync();

            if (!readonlyLibraryIds.Any())
                return Enumerable.Empty<ComponentPropertyOptionThreatMapping>();

            var libraryIdList = readonlyLibraryIds.ToList();
            var libraryParameters = string.Join(",", libraryIdList.Select((_, i) => $"@lib{i}"));

            var sql = $@"{BuildMappingSelectQuery()} 
                            WHERE (p.LibraryId IN ({libraryParameters}) OR c.LibraryId IN ({libraryParameters}) OR t.LibraryId IN ({libraryParameters}))";

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
            return @"SELECT cpotm.Id, cpotm.IsHidden, cpotm.IsOverridden, 
                            c.Guid as ComponentGuid, p.Guid as PropertyGuid, po.Guid as PropertyOptionGuid, t.Guid as ThreatGuid
                        FROM ComponentPropertyOptionThreatMapping cpotm
                        INNER JOIN ComponentPropertyOptionMapping cpom ON cpotm.ComponentPropertyOptionId = cpom.Id
                        INNER JOIN ComponentPropertyMapping cp ON cpom.ComponentPropertyId = cp.Id
                        INNER JOIN Components c ON cp.ComponentId = c.Id
                        INNER JOIN Properties p ON cp.PropertyId = p.Id
                        INNER JOIN PropertyOptions po ON cpom.PropertyOptionId = po.Id
                        INNER JOIN Threats t ON cpotm.ThreatId = t.Id";
        }

        private async Task<IEnumerable<ComponentPropertyOptionThreatMapping>> ExecuteMappingReaderAsync(SqlCommand command)
        {
            var mappings = new List<ComponentPropertyOptionThreatMapping>();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                mappings.Add(new ComponentPropertyOptionThreatMapping
                {
                    Id = (int)reader["Id"],
                    ComponentGuid = (Guid)reader["ComponentGuid"],
                    PropertyGuid = (Guid)reader["PropertyGuid"],
                    PropertyOptionGuid = (Guid)reader["PropertyOptionGuid"],
                    ThreatGuid = (Guid)reader["ThreatGuid"],
                    IsHidden = (bool)reader["isHidden"],
                    IsOverridden = (bool)reader["IsOverridden"]
                });
            }

            return mappings;
        }
    }
}
