using Microsoft.Data.SqlClient;
using ThreatFramework.Core.Models.PropertyMapping;
using ThreatFramework.Infra.Contract;
using ThreatFramework.Infra.Contract.Repository;

namespace ThreatFramework.Infrastructure.Repository
{
    public class ComponentPropertyMappingRepository : IComponentPropertyMappingRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;
        private readonly ILibraryCacheService _libraryCacheService;

        public ComponentPropertyMappingRepository(ISqlConnectionFactory connectionFactory, ILibraryCacheService libraryCacheService)
        {
            _connectionFactory = connectionFactory;
            _libraryCacheService = libraryCacheService;
        }

        public async Task<IEnumerable<ComponentPropertyMapping>> GetMappingsByLibraryIdAsync(IEnumerable<Guid> libraryGuids)
        {
            var libraryIds = await _libraryCacheService.GetIdsFromGuid(libraryGuids);

            if (!libraryIds.Any())
                return Enumerable.Empty<ComponentPropertyMapping>();

            var libraryIdList = libraryIds.ToList();
            var libraryParameters = string.Join(",", libraryIdList.Select((_, i) => $"@lib{i}"));

            var sql = $@"{BuildMappingSelectQuery()} 
                        WHERE (p.LibraryId IN ({libraryParameters}) OR c.LibraryId IN ({libraryParameters}))";

            using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            using var command = new SqlCommand(sql, connection);

            for (int i = 0; i < libraryIdList.Count; i++)
            {
                command.Parameters.AddWithValue($"@lib{i}", libraryIdList[i]);
            }

            return await ExecuteMappingReaderAsync(command);
        }

        public async Task<IEnumerable<ComponentPropertyMapping>> GetReadOnlyMappingsAsync()
        {
            var readonlyLibraryIds = await _libraryCacheService.GetReadOnlyLibraryIdAsync();

            if (!readonlyLibraryIds.Any())
                return Enumerable.Empty<ComponentPropertyMapping>();

            var libraryIdList = readonlyLibraryIds.ToList();
            var libraryParameters = string.Join(",", libraryIdList.Select((_, i) => $"@lib{i}"));

            var sql = $@"{BuildMappingSelectQuery()} 
                        WHERE (p.LibraryId IN ({libraryParameters}) OR c.LibraryId IN ({libraryParameters}))";

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
            return @"SELECT cpm.Id, cpm.IsOptional, cpm.isHidden, cpm.IsOverridden, p.Guid as PropertyGuid, c.Guid as ComponentGuid
                    FROM ComponentPropertyMapping cpm
                    INNER JOIN Properties p ON cpm.PropertyId = p.Id
                    INNER JOIN Components c ON cpm.ComponentId = c.Id";
        }

        private async Task<IEnumerable<ComponentPropertyMapping>> ExecuteMappingReaderAsync(SqlCommand command)
        {
            var mappings = new List<ComponentPropertyMapping>();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                mappings.Add(new ComponentPropertyMapping
                {
                    Id = (int)reader["Id"],
                    PropertyGuid = (Guid)reader["PropertyGuid"],
                    ComponentGuid = (Guid)reader["ComponentGuid"],
                    IsOptional = (bool)reader["IsOptional"],
                    IsHidden = (bool)reader["isHidden"],
                    IsOverridden = (bool)reader["IsOverridden"]
                });
            }

            return mappings;
        }
    }
}
