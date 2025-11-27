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
                        WHERE c.LibraryId IN ({libraryParameters})";

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
                WHERE c.LibraryId IN ({libraryParameters})";

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
            return @"SELECT c.Id, c.Guid, c.LibraryId, c.ComponentTypeId, ct.Guid AS ComponentTypeGuid, c.isHidden, c.IsOverriden, 
                    c.CreatedDate, c.LastUpdated, c.Name, c.ImagePath, c.Labels, 
                    c.Version, c.Description, c.ChineseDescription 
            FROM Components c
            INNER JOIN ComponentTypes ct ON c.ComponentTypeId = ct.Id";
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
                    LibraryGuid = await _libraryCacheService.GetGuidByIdAsync((int)reader["LibraryId"]),
                    ComponentTypeGuid = (Guid)reader["ComponentTypeGuid"],
                    IsHidden = (bool)reader["isHidden"],
                    IsOverridden = (bool)reader["IsOverriden"],
                    CreatedDate = (DateTime)reader["CreatedDate"],
                    LastUpdated = reader["LastUpdated"] as DateTime?,
                    Name = (string)reader["Name"],
                    ImagePath = reader["ImagePath"] as string,
                    Labels = reader["Labels"] as string,
                    Version = reader["Version"] as string,
                    Description = reader["Description"] as string,
                    ChineseDescription = reader["ChineseDescription"] as string,
                });
            }

            return components;
        }

        public async Task<IEnumerable<Guid>> GetGuidsAsync()
        {
            var sql = "SELECT Guid FROM Components";

            using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            using var command = new SqlCommand(sql, connection);

            var guids = new List<Guid>();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                guids.Add((Guid)reader["Guid"]);
            }

            return guids;
        }

        public async Task<IEnumerable<(Guid ComponentGuid, Guid LibraryGuid)>> GetGuidsAndLibraryGuidsAsync()
        {
            const string sql = @"
        SELECT c.Guid AS ComponentGuid, l.Guid AS LibraryGuid
        FROM Components c
        INNER JOIN Libraries l ON c.LibraryId = l.Id";

            using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            using var command = new SqlCommand(sql, connection);

            var results = new List<(Guid, Guid)>();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var componentGuid = reader.GetGuid(reader.GetOrdinal("ComponentGuid"));
                var libraryGuid = reader.GetGuid(reader.GetOrdinal("LibraryGuid"));

                results.Add((componentGuid, libraryGuid));
            }

            return results;
        }

        public async Task<IEnumerable<Guid>> GetGuidsByLibraryIds(IEnumerable<Guid> libraryIds)
        {
            // Translate library GUIDs to their integer IDs (cached)
            var ids = await _libraryCacheService.GetIdsFromGuid(libraryIds);

            if (!ids.Any())
                return Enumerable.Empty<Guid>();

            var libraryIdList = ids.ToList();
            var libraryParameters = string.Join(",", libraryIdList.Select((_, i) => $"@lib{i}"));

            var sql = $@"SELECT Guid 
                 FROM Components 
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
