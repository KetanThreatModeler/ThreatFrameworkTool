using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Infra.Contract.Repository;
using ThreatFramework.Core.Cache;
using ThreatFramework.Core.CoreEntities;

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
            var libraries = new List<LibraryCache>();
            using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            var sql = @"SELECT Id, Guid, Readonly
                        FROM Libraries s
                        ORDER BY Name";
            
            using var command = new SqlCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();
            
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
            var sql = @"SELECT Id, Guid, DepartmentId, DateCreated, LastUpdated, Readonly, IsDefault, 
                       Name, SharingType, Description, Labels, Version, ReleaseNotes, ImageURL
                FROM Libraries 
                WHERE Readonly = 1
                ORDER BY Name";
    
            return await ExecuteLibraryQueryAsync(sql);
        }

        public async Task<IEnumerable<Library>> GetLibrariesByGuidsAsync(IEnumerable<Guid> guids)
        {
            if (!guids.Any())
                return Enumerable.Empty<Library>();
    
            var guidList = guids.ToList();
            var guidParameters = string.Join(",", guidList.Select((_, i) => $"@guid{i}"));
    
            var sql = @$"SELECT Id, Guid, DepartmentId, DateCreated, LastUpdated, Readonly, IsDefault, 
                        Name, SharingType, Description, Labels, Version, ReleaseNotes, ImageURL
                 FROM Libraries 
                 WHERE Guid IN ({guidParameters})
                 ORDER BY Name";
    
            using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            using var command = new SqlCommand(sql, connection);
    
            for (int i = 0; i < guidList.Count; i++)
            {
                command.Parameters.AddWithValue($"@guid{i}", guidList[i]);
            }
    
            return await ExecuteLibraryReaderAsync(command);
        }

        private async Task<IEnumerable<Library>> ExecuteLibraryQueryAsync(string sql)
        {
            using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            using var command = new SqlCommand(sql, connection);
    
            return await ExecuteLibraryReaderAsync(command);
        }

        private async Task<IEnumerable<Library>> ExecuteLibraryReaderAsync(SqlCommand command)
        {
            var libraries = new List<Library>();
            using var reader = await command.ExecuteReaderAsync();
    
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
    }
}
