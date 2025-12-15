using Microsoft.Data.SqlClient;
using ThreatFramework.Infra.Contract;
using ThreatFramework.Infra.Contract.Repository;
using ThreatModeler.TF.Core.Model.PropertyMapping;

namespace ThreatFramework.Infrastructure.Repository
{
    public class ComponentPropertyOptionMappingRepository : IComponentPropertyOptionMappingRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;
        private readonly ILibraryCacheService _libraryCacheService;

        public ComponentPropertyOptionMappingRepository(ISqlConnectionFactory connectionFactory, ILibraryCacheService libraryCacheService)
        {
            _connectionFactory = connectionFactory;
            _libraryCacheService = libraryCacheService;
        }

        public async Task<IEnumerable<ComponentPropertyOptionMapping>> GetMappingsByLibraryGuidAsync(IEnumerable<Guid> libraryGuids)
        {
            var libraryIds = await _libraryCacheService.GetIdsFromGuid(libraryGuids);

            if (!libraryIds.Any())
                return Enumerable.Empty<ComponentPropertyOptionMapping>();

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

        public async Task<IEnumerable<ComponentPropertyOptionMapping>> GetReadOnlyMappingsAsync()
        {
            var readonlyLibraryIds = await _libraryCacheService.GetReadOnlyLibraryIdAsync();

            if (!readonlyLibraryIds.Any())
                return Enumerable.Empty<ComponentPropertyOptionMapping>();

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
            return @"SELECT cpom.Id, cpom.IsDefault, cpom.isHidden, cpom.IsOverridden, 
                            c.Guid as ComponentGuid, p.Guid as PropertyGuid, po.Guid as PropertyOptionGuid
                        FROM ComponentPropertyOptionMapping cpom
                        INNER JOIN ComponentPropertyMapping cp ON cpom.ComponentPropertyId = cp.Id
                        INNER JOIN Components c ON cp.ComponentId = c.Id
                        INNER JOIN Properties p ON cp.PropertyId = p.Id
                        INNER JOIN PropertyOptions po ON cpom.PropertyOptionId = po.Id";
        }

        private async Task<IEnumerable<ComponentPropertyOptionMapping>> ExecuteMappingReaderAsync(SqlCommand command)
        {
            var mappings = new List<ComponentPropertyOptionMapping>();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                mappings.Add(new ComponentPropertyOptionMapping
                {
                    Id = (int)reader["Id"],
                    ComponentGuid = (Guid)reader["ComponentGuid"],
                    PropertyGuid = (Guid)reader["PropertyGuid"],
                    PropertyOptionGuid = (Guid)reader["PropertyOptionGuid"],
                    IsDefault = (bool)reader["IsDefault"],
                    IsHidden = (bool)reader["isHidden"],
                    IsOverridden = (bool)reader["IsOverridden"]
                });
            }

            return mappings;
        }
    }
}
