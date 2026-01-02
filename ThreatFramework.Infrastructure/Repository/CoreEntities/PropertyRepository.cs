using Microsoft.Data.SqlClient;
using ThreatFramework.Infra.Contract;
using ThreatModeler.TF.Core.Model.CoreEntities;
using ThreatModeler.TF.Infra.Contract.Repository.CoreEntities;
using ThreatModeler.TF.Infra.Implmentation.Helper;

namespace ThreatModeler.TF.Infra.Implmentation.Repository.CoreEntities
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
            return @"
        SELECT
            p.Id,
            p.LibraryId,
            p.PropertyTypeId,
            p.[isSelected] AS IsSelected,
            p.IsOptional,
            p.[isHidden] AS IsHidden,
            p.LastUpdated,
            p.Guid,
            p.Name,
            p.ChineseName,
            p.Labels,
            p.Description,
            p.ChineseDescription,
            pt.Guid AS PropertyTypeGuid,
            pt.Name AS PropertyTypeName
        FROM Properties p
        INNER JOIN PropertyTypes pt ON p.PropertyTypeId = pt.Id";
        }


        private async Task<IEnumerable<Property>> ExecutePropertyReaderAsync(SqlCommand command)
        {
            var properties = new List<Property>();
            using var reader = await command.ExecuteReaderAsync();

            // Cache ordinals (fails fast if select/query mismatches)
            int ordLibraryId = reader.GetOrdinal("LibraryId");
            int ordPropertyTypeGuid = reader.GetOrdinal("PropertyTypeGuid");
            int ordPropertyTypeName = reader.GetOrdinal("PropertyTypeName");
            int ordIsSelected = reader.GetOrdinal("IsSelected");   // from alias
            int ordIsOptional = reader.GetOrdinal("IsOptional");
            int ordIsHidden = reader.GetOrdinal("IsHidden");       // from alias
            int ordGuid = reader.GetOrdinal("Guid");
            int ordName = reader.GetOrdinal("Name");
            int ordChineseName = reader.GetOrdinal("ChineseName");
            int ordLabels = reader.GetOrdinal("Labels");
            int ordDescription = reader.GetOrdinal("Description");
            int ordChineseDescription = reader.GetOrdinal("ChineseDescription");

            while (await reader.ReadAsync())
            {
                var sqlLibraryId = reader.GetInt32(ordLibraryId);
                var libraryGuid = await _libraryCacheService.GetGuidByIdAsync(sqlLibraryId);

                properties.Add(new Property
                {
                    LibraryGuid = libraryGuid,
                    PropertyTypeGuid = reader.GetGuid(ordPropertyTypeGuid),
                    PropertyTypeName = reader.IsDBNull(ordPropertyTypeName) ? null : reader.GetString(ordPropertyTypeName),

                    IsSelected = !reader.IsDBNull(ordIsSelected) && reader.GetBoolean(ordIsSelected),
                    IsOptional = !reader.IsDBNull(ordIsOptional) && reader.GetBoolean(ordIsOptional),

                    // Not present in the table script -> keep domain compatibility
                    IsGlobal = false,
                    IsOverridden = false,

                    IsHidden = !reader.IsDBNull(ordIsHidden) && reader.GetBoolean(ordIsHidden),

                    Guid = reader.GetGuid(ordGuid),
                    Name = reader.IsDBNull(ordName) ? null : reader.GetString(ordName),
                    ChineseName = reader.IsDBNull(ordChineseName) ? null : reader.GetString(ordChineseName),

                    Labels = reader.IsDBNull(ordLabels) ? null : reader.GetValue(ordLabels)?.ToString().ToLabelList(),

                    Description = reader.IsDBNull(ordDescription) ? null : reader.GetString(ordDescription),
                    ChineseDescription = reader.IsDBNull(ordChineseDescription) ? null : reader.GetString(ordChineseDescription)
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

        public async Task<IEnumerable<(Guid PropertyGuid, Guid LibraryGuid)>> GetGuidsAndLibraryGuidsAsync(IEnumerable<Guid> libraryIds)
        {
            if (libraryIds == null || !libraryIds.Any())
                return Enumerable.Empty<(Guid PropertyGuid, Guid LibraryGuid)>();

            // Convert library GUIDs to integer IDs used in DB
            var ids = await _libraryCacheService.GetIdsFromGuid(libraryIds);

            if (!ids.Any())
                return Enumerable.Empty<(Guid PropertyGuid, Guid LibraryGuid)>();

            var libraryIdList = ids.ToList();
            var libraryParameters = string.Join(",", libraryIdList.Select((_, i) => $"@lib{i}"));

            var sql = $@"
        SELECT p.Guid AS PropertyGuid, l.Guid AS LibraryGuid
        FROM Properties p
        INNER JOIN Libraries l ON p.LibraryId = l.Id
        WHERE p.LibraryId IN ({libraryParameters})";

            using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            using var command = new SqlCommand(sql, connection);

            for (int i = 0; i < libraryIdList.Count; i++)
            {
                command.Parameters.AddWithValue($"@lib{i}", libraryIdList[i]);
            }

            var results = new List<(Guid PropertyGuid, Guid LibraryGuid)>();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var propertyGuid = reader.GetGuid(reader.GetOrdinal("PropertyGuid"));
                var libraryGuid = reader.GetGuid(reader.GetOrdinal("LibraryGuid"));

                results.Add((propertyGuid, libraryGuid));
            }

            return results;
        }

        public async Task<IEnumerable<Property>> GetAllPropertiesAsync()
        {
            var sql = BuildPropertySelectQuery();

            using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            using var command = new SqlCommand(sql, connection);

            return await ExecutePropertyReaderAsync(command);
        }

    }
}
