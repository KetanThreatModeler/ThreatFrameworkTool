using Microsoft.Data.SqlClient;
using ThreatFramework.Core.CoreEntities;
using ThreatFramework.Infra.Contract;
using ThreatFramework.Infra.Contract.Repository;

namespace ThreatFramework.Infrastructure.Repository
{
    public class PropertyRepository : IPropertyRepository
    {
        private readonly ILibraryCacheService _libraryCacheService;
        private readonly ISqlConnectionFactory _connectionFactory;

        public PropertyRepository(ILibraryCacheService libraryCacheService, ISqlConnectionFactory sqlConnectionFactory)
        {
            _libraryCacheService = libraryCacheService;
            _connectionFactory = sqlConnectionFactory;
        }

        public async Task<IEnumerable<Property>> GetReadOnlyPropertiesAsync()
        {
            var readonlyLibraryIds = await _libraryCacheService.GetReadOnlyLibraryIdAsync();

            if (!readonlyLibraryIds.Any())
                return Enumerable.Empty<Property>();

            var libraryIdList = readonlyLibraryIds.ToList();
            var libraryParameters = string.Join(",", libraryIdList.Select((_, i) => $"@lib{i}"));

            var sql = $@"{BuildPropertySelectQuery()} 
                        WHERE LibraryId IN ({libraryParameters})";

            using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            using var command = new SqlCommand(sql, connection);

            for (int i = 0; i < libraryIdList.Count; i++)
            {
                command.Parameters.AddWithValue($"@lib{i}", libraryIdList[i]);
            }

            return await ExecutePropertyReaderAsync(command);
        }

        public async Task<IEnumerable<Property>> GetPropertiesByLibraryIdAsync(IEnumerable<Guid> libraryIds)
        {
            var ids = await _libraryCacheService.GetIdsFromGuid(libraryIds);

            if (!ids.Any())
                return Enumerable.Empty<Property>();

            var libraryIdList = ids.ToList();
            var libraryParameters = string.Join(",", libraryIdList.Select((_, i) => $"@lib{i}"));

            var sql = $@"{BuildPropertySelectQuery()} 
                        WHERE LibraryId IN ({libraryParameters})";

            using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            using var command = new SqlCommand(sql, connection);

            for (int i = 0; i < libraryIdList.Count; i++)
            {
                command.Parameters.AddWithValue($"@lib{i}", libraryIdList[i]);
            }

            return await ExecutePropertyReaderAsync(command);
        }

        private static string BuildPropertySelectQuery()
        {
            return @"SELECT p.Id, p.LibraryId, p.PropertyTypeId, p.isSelected, p.IsOptional, p.IsGlobal, 
                            p.isHidden, p.IsOverridden, p.CreatedDate, p.LastUpdated, p.Guid, p.Name, 
                            p.ChineseName, p.Labels, p.Description, p.ChineseDescription,
                            pt.Guid AS PropertyTypeGuid, pt.Name AS PropertyTypeName
                    FROM Properties p
                    INNER JOIN PropertyTypes pt ON p.PropertyTypeId = pt.Id";
        }

        private async Task<IEnumerable<Property>> ExecutePropertyReaderAsync(SqlCommand command)
        {
            var properties = new List<Property>();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                properties.Add(new Property
                {
                    Id = (int)reader["Id"],
                    LibraryGuid = await _libraryCacheService.GetGuidByIdAsync((int)reader["LibraryId"]),
                    PropertyTypeGuid = (Guid)reader["PropertyTypeGuid"],
                    PropertyTypeName = reader["PropertyTypeName"] as string,
                    IsSelected = (bool)reader["isSelected"],
                    IsOptional = (bool)reader["IsOptional"],
                    IsGlobal = (bool)reader["IsGlobal"],
                    IsHidden = (bool)reader["isHidden"],
                    IsOverridden = (bool)reader["IsOverridden"],
                    CreatedDate = (DateTime)reader["CreatedDate"],
                    LastUpdated = reader["LastUpdated"] as DateTime?,
                    Guid = (Guid)reader["Guid"],
                    Name = reader["Name"] as string,
                    ChineseName = reader["ChineseName"] as string,
                    Labels = reader["Labels"] as string,
                    Description = reader["Description"] as string,
                    ChineseDescription = reader["ChineseDescription"] as string
                });
            }

            return properties;
        }

        public async Task<IEnumerable<Guid>> GetGuidsAsync()
        {
            var sql = "SELECT Guid FROM Properties";

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

        public async Task<IEnumerable<(Guid PropertyGuid, Guid LibraryGuid)>> GetGuidsAndLibraryGuidsAsync()
        {
            const string sql = @"
        SELECT p.Guid AS PropertyGuid, l.Guid AS LibraryGuid
        FROM Properties p
        INNER JOIN Libraries l ON p.LibraryId = l.Id";

            using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            using var command = new SqlCommand(sql, connection);

            var results = new List<(Guid, Guid)>();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var propertyGuid = reader.GetGuid(reader.GetOrdinal("PropertyGuid"));
                var libraryGuid = reader.GetGuid(reader.GetOrdinal("LibraryGuid"));

                results.Add((propertyGuid, libraryGuid));
            }

            return results;
        }

        public async Task<IEnumerable<Guid>> GetGuidsByLibraryIds(IEnumerable<Guid> libraryIds)
        {
            if (libraryIds == null || !libraryIds.Any())
                return Enumerable.Empty<Guid>();

            // Convert library GUIDs to integer IDs used in DB
            var ids = await _libraryCacheService.GetIdsFromGuid(libraryIds);

            if (!ids.Any())
                return Enumerable.Empty<Guid>();

            var libraryIdList = ids.ToList();
            var libraryParameters = string.Join(",", libraryIdList.Select((_, i) => $"@lib{i}"));

            var sql = $@"SELECT Guid 
                 FROM Properties
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
