using Microsoft.Data.SqlClient;
using ThreatFramework.Core.CoreEntities;
using ThreatFramework.Infra.Contract;
using ThreatFramework.Infra.Contract.Repository;

namespace ThreatFramework.Infrastructure.Repository
{
    public class ComponentRepository : IComponentRepository
    {
        private readonly ILibraryCacheService _libraryCacheService;
        private readonly ISqlConnectionFactory _connectionFactory;

        public ComponentRepository(ILibraryCacheService libraryCacheService, ISqlConnectionFactory sqlConnectionFactory)
        {
            _libraryCacheService = libraryCacheService;
            _connectionFactory = sqlConnectionFactory;
        }

        public async Task<IEnumerable<Component>> GetReadOnlyComponentsAsync()
        {
            var readonlyLibraryIds = await _libraryCacheService.GetReadOnlyLibraryIdAsync();

            if (!readonlyLibraryIds.Any())
                return Enumerable.Empty<Component>();

            var libraryIdList = readonlyLibraryIds.ToList();
            var libraryParameters = string.Join(",", libraryIdList.Select((_, i) => $"@lib{i}"));

            var sql = $@"{BuildComponentSelectQuery()} 
                        WHERE LibraryId IN ({libraryParameters})";

            using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            using var command = new SqlCommand(sql, connection);

            for (int i = 0; i < libraryIdList.Count; i++)
            {
                command.Parameters.AddWithValue($"@lib{i}", libraryIdList[i]);
            }

            return await ExecuteComponentReaderAsync(command);
        }

        public async Task<IEnumerable<Component>> GetComponentsByLibraryIdAsync(IEnumerable<Guid> libraryIds)
        {
            var ids = await _libraryCacheService.GetIdsFromGuid(libraryIds);

            if (!ids.Any())
                return Enumerable.Empty<Component>();

            var libraryIdList = ids.ToList();
            var libraryParameters = string.Join(",", libraryIdList.Select((_, i) => $"@lib{i}"));

            var sql = $@"{BuildComponentSelectQuery()} 
                WHERE LibraryId IN ({libraryParameters})";

            using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            using var command = new SqlCommand(sql, connection);

            for (int i = 0; i < libraryIdList.Count; i++)
            {
                command.Parameters.AddWithValue($"@lib{i}", libraryIdList[i]);
            }

            return await ExecuteComponentReaderAsync(command);
        }

        private static string BuildComponentSelectQuery()
        {
            return @"SELECT c.Id, c.Guid, c.LibraryId, c.ComponentTypeId, c.isHidden, c.IsOverriden, 
                            c.CreatedDate, c.LastUpdated, c.Name, c.ImagePath, c.Labels, 
                            c.Version, c.Description, c.ChineseDescription 
                    FROM Components c";
        }

        private async Task<IEnumerable<Component>> ExecuteComponentReaderAsync(SqlCommand command)
        {
            var components = new List<Component>();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                components.Add(new Component
                {
                    Id = (int)reader["Id"],
                    Guid = (Guid)reader["Guid"],
                    LibraryId = await _libraryCacheService.GetGuidByIdAsync((int)reader["LibraryId"]),
                    ComponentTypeId = (int)reader["ComponentTypeId"],
                    IsHidden = (bool)reader["isHidden"],
                    IsOverridden = (bool)reader["IsOverriden"],
                    CreatedDate = (DateTime)reader["CreatedDate"],
                    LastUpdated = reader["LastUpdated"] as DateTime?,
                    Name = (string)reader["Name"],
                    ImagePath = reader["ImagePath"] as string,
                    Labels = reader["Labels"] as string,
                    Version = reader["Version"] as string,
                    Description = reader["Description"] as string,
                    ChineseDescription = reader["ChineseDescription"] as string
                });
            }

            return components;
        }
    }
}
