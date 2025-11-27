using Microsoft.Data.SqlClient;
using ThreatFramework.Core.Cache;
using ThreatFramework.Core.CoreEntities;
using ThreatFramework.Infra.Contract.Repository;

namespace ThreatFramework.Infrastructure.Repository
{
    public class LibraryRepository : ILibraryRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public LibraryRepository(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<IEnumerable<LibraryCache>> GetLibrariesCacheAsync()
        {
            List<LibraryCache> libraries = [];
            using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync();
            string sql = @"SELECT Id, Guid, Readonly
                        FROM Libraries s
                        ORDER BY Name";

            using SqlCommand command = new(sql, connection);
            using SqlDataReader reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                libraries.Add(new LibraryCache
                {
                    Id = (int)reader["Id"],
                    Guid = (Guid)reader["Guid"],
                    IsReadonly = (bool)reader["Readonly"]
                });
            }

            return libraries;
        }

        public async Task<IEnumerable<Library>> GetReadonlyLibrariesAsync()
        {
            string sql = @"SELECT Id, Guid, DepartmentId, DateCreated, LastUpdated, Readonly, IsDefault, 
                       Name, SharingType, Description, Labels, Version, ReleaseNotes, ImageURL
                FROM Libraries 
                WHERE Readonly = 1
                ORDER BY Name";

            return await ExecuteLibraryQueryAsync(sql);
        }

        public async Task<IEnumerable<Library>> GetLibrariesByGuidsAsync(IEnumerable<Guid> guids)
        {
            if (!guids.Any())
            {
                return Enumerable.Empty<Library>();
            }

            List<Guid> guidList = guids.ToList();
            string guidParameters = string.Join(",", guidList.Select((_, i) => $"@guid{i}"));

            string sql = @$"SELECT Id, Guid, DepartmentId, DateCreated, LastUpdated, Readonly, IsDefault, 
                        Name, SharingType, Description, Labels, Version, ReleaseNotes, ImageURL
                 FROM Libraries 
                 WHERE Guid IN ({guidParameters})
                 ORDER BY Name";

            using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync();
            using SqlCommand command = new(sql, connection);

            for (int i = 0; i < guidList.Count; i++)
            {
                _ = command.Parameters.AddWithValue($"@guid{i}", guidList[i]);
            }

            return await ExecuteLibraryReaderAsync(command);
        }

        private async Task<IEnumerable<Library>> ExecuteLibraryQueryAsync(string sql)
        {
            using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync();
            using SqlCommand command = new(sql, connection);

            return await ExecuteLibraryReaderAsync(command);
        }

        private async Task<IEnumerable<Library>> ExecuteLibraryReaderAsync(SqlCommand command)
        {
            List<Library> libraries = [];
            using SqlDataReader reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                libraries.Add(new Library
                {
                    Id = (int)reader["Id"],
                    Guid = (Guid)reader["Guid"],
                    DepartmentId = (int)reader["DepartmentId"],
                    DateCreated = (DateTime)reader["DateCreated"],
                    LastUpdated = (DateTime)reader["LastUpdated"],
                    Readonly = (bool)reader["Readonly"],
                    IsDefault = (bool)reader["IsDefault"],
                    Name = (string)reader["Name"],
                    SharingType = reader["SharingType"] as string,
                    Description = reader["Description"] as string,
                    Labels = reader["Labels"] as string,
                    Version = reader["Version"] as string,
                    ReleaseNotes = reader["ReleaseNotes"] as string,
                    ImageURL = reader["ImageURL"] as string
                });
            }

            return libraries;
        }

        public async Task<IEnumerable<Guid>> GetLibraryGuidsAsync()
        {
            List<Guid> guids = [];
            using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync();
            string sql = @"SELECT Guid FROM Libraries";

            using SqlCommand command = new(sql, connection);
            using SqlDataReader reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                guids.Add((Guid)reader["Guid"]);
            }

            return guids;
        }

        public async Task<IEnumerable<Guid>> GetGuidsByLibraryIds(IEnumerable<Guid> libraryIds)
        {
            if (libraryIds == null || !libraryIds.Any())
            {
                return Enumerable.Empty<Guid>();
            }

            List<Guid> guidList = libraryIds.ToList();
            string guidParameters = string.Join(",", guidList.Select((_, i) => $"@guid{i}"));

            string sql = @$"SELECT Guid
                    FROM Libraries
                    WHERE Guid IN ({guidParameters})
                    ORDER BY Name";

            using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync();
            using SqlCommand command = new(sql, connection);

            for (int i = 0; i < guidList.Count; i++)
            {
                _ = command.Parameters.AddWithValue($"@guid{i}", guidList[i]);
            }

            List<Guid> result = [];
            using SqlDataReader reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                result.Add((Guid)reader["Guid"]);
            }

            return result;
        }
    }
}
