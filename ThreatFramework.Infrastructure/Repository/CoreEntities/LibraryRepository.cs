using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;
using ThreatFramework.Core.Cache;
using ThreatFramework.Infra.Contract;
using ThreatModeler.TF.Core.Model.CoreEntities;
using ThreatModeler.TF.Infra.Contract.Repository.CoreEntities;
using ThreatModeler.TF.Infra.Implmentation.Helper;

namespace ThreatModeler.TF.Infra.Implmentation.Repository.CoreEntities
{
    public class LibraryRepository : ILibraryRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;
        private readonly ILogger<LibraryRepository> _logger;

        private const int DefaultCommandTimeoutSeconds = 30;

        public LibraryRepository(
            ISqlConnectionFactory connectionFactory,
            ILogger<LibraryRepository> logger)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<LibraryCache>> GetLibrariesCacheAsync()
        {
            const string methodName = nameof(GetLibrariesCacheAsync);
            _logger.LogInformation("{Method} - Starting execution.", methodName);

            const string sql = @"
                SELECT Id, Guid, Readonly
                FROM Libraries
                ORDER BY Name;";

            using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync();
            using SqlCommand command = CreateCommand(connection, sql);

            var libraries = new List<LibraryCache>();

            using SqlDataReader reader = await command.ExecuteReaderAsync();

            int ordId = reader.GetOrdinal("Id");
            int ordGuid = reader.GetOrdinal("Guid");
            int ordReadonly = reader.GetOrdinal("Readonly");

            while (await reader.ReadAsync())
            {
                libraries.Add(new LibraryCache
                {
                    Id = reader.GetInt32(ordId),
                    Guid = reader.GetGuid(ordGuid),
                    IsReadonly = reader.GetBoolean(ordReadonly)
                });
            }

            _logger.LogInformation("{Method} - Successfully retrieved {Count} library cache entries.",
                methodName, libraries.Count);

            return libraries;
        }

        public async Task<IEnumerable<Library>> GetReadonlyLibrariesAsync()
        {
            const string methodName = nameof(GetReadonlyLibrariesAsync);
            _logger.LogInformation("{Method} - Starting execution.", methodName);

            const string sql = @"
                SELECT Id, Guid, DepartmentId, DateCreated, LastUpdated, Readonly, IsDefault,
                       Name, SharingType, Description, Labels, Version, ReleaseNotes, ImageURL
                FROM Libraries
                WHERE Readonly = 1
                ORDER BY Name;";

            var result = await ExecuteLibraryQueryAsync(sql);

            _logger.LogInformation("{Method} - Successfully retrieved {Count} readonly libraries.",
                methodName, result.Count);

            return result;
        }

        public async Task<IEnumerable<Library>> GetLibrariesByGuidsAsync(IEnumerable<Guid> guids)
        {
            const string methodName = nameof(GetLibrariesByGuidsAsync);

            if (guids == null)
                throw new ArgumentNullException(nameof(guids));

            var guidList = guids as IList<Guid> ?? guids.ToList();
            if (guidList.Count == 0)
            {
                _logger.LogInformation("{Method} - No GUIDs provided. Returning empty result.", methodName);
                return Enumerable.Empty<Library>();
            }

            string guidParameters = string.Join(",", guidList.Select((_, i) => $"@guid{i}"));

            string sql = @$"
                SELECT Id, Guid, DepartmentId, DateCreated, LastUpdated, Readonly, IsDefault,
                       Name, SharingType, Description, Labels, Version, ReleaseNotes, ImageURL
                FROM Libraries
                WHERE Guid IN ({guidParameters})
                ORDER BY Name;";

            _logger.LogDebug("{Method} - Executing SQL with {Count} GUIDs. SQL: {Sql}",
                methodName, guidList.Count, sql);

            using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync();
            using SqlCommand command = CreateCommand(connection, sql);

            AddGuidParameters(command, guidList);

            var result = await ExecuteLibraryReaderStrictAsync(command);

            _logger.LogInformation("{Method} - Successfully retrieved {Count} libraries by GUID.",
                methodName, result.Count);

            return result;
        }

        public async Task<IEnumerable<Guid>> GetLibraryGuidsAsync()
        {
            const string methodName = nameof(GetLibraryGuidsAsync);
            _logger.LogInformation("{Method} - Starting execution.", methodName);

            const string sql = @"SELECT Guid FROM Libraries;";

            using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync();
            using SqlCommand command = CreateCommand(connection, sql);

            var guids = new List<Guid>();

            using SqlDataReader reader = await command.ExecuteReaderAsync();
            int ordGuid = reader.GetOrdinal("Guid");

            while (await reader.ReadAsync())
            {
                guids.Add(reader.GetGuid(ordGuid));
            }

            _logger.LogInformation("{Method} - Successfully retrieved {Count} library GUIDs.",
                methodName, guids.Count);

            return guids;
        }

        public async Task<IEnumerable<(Guid LibGuid, Guid LibraryGuid)>> GetLibraryGuidsWithLibGuidAsync()
        {
            // Same business logic: just return (g, g) for each library guid.
            var guids = await GetLibraryGuidsAsync();
            return guids.Select(g => (LibGuid: g, LibraryGuid: g));
        }

        public async Task<IEnumerable<Guid>> GetGuidsByLibraryIds(IEnumerable<Guid> libraryIds)
        {
            const string methodName = nameof(GetGuidsByLibraryIds);

            if (libraryIds == null)
                throw new ArgumentNullException(nameof(libraryIds));

            var guidList = libraryIds as IList<Guid> ?? libraryIds.ToList();
            if (guidList.Count == 0)
            {
                _logger.LogInformation("{Method} - No library GUIDs provided. Returning empty result.", methodName);
                return Enumerable.Empty<Guid>();
            }

            string guidParameters = string.Join(",", guidList.Select((_, i) => $"@guid{i}"));

            string sql = @$"
                SELECT Guid
                FROM Libraries
                WHERE Guid IN ({guidParameters})
                ORDER BY Name;";

            _logger.LogDebug("{Method} - Executing SQL with {Count} GUIDs. SQL: {Sql}",
                methodName, guidList.Count, sql);

            using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync();
            using SqlCommand command = CreateCommand(connection, sql);

            AddGuidParameters(command, guidList);

            var result = new List<Guid>();

            using SqlDataReader reader = await command.ExecuteReaderAsync();
            int ordGuid = reader.GetOrdinal("Guid");

            while (await reader.ReadAsync())
            {
                result.Add(reader.GetGuid(ordGuid));
            }

            _logger.LogInformation("{Method} - Successfully retrieved {Count} GUIDs.",
                methodName, result.Count);

            return result;
        }

        public async Task<IEnumerable<(Guid LibGuid, Guid LibraryGuid)>> GetGuidsAndLibraryGuidsAsync(IEnumerable<Guid> libraryIds)
        {
            if (libraryIds == null)
                throw new ArgumentNullException(nameof(libraryIds));

            var guids = await GetGuidsByLibraryIds(libraryIds);
            return guids.Select(g => (LibGuid: g, LibraryGuid: g));
        }

        // --------------------------
        // Private helpers (STRICT)
        // --------------------------

        private SqlCommand CreateCommand(SqlConnection connection, string sql)
        {
            return new SqlCommand(sql, connection)
            {
                CommandType = CommandType.Text,
                CommandTimeout = DefaultCommandTimeoutSeconds
            };
        }

        private static void AddGuidParameters(SqlCommand command, IList<Guid> guidList)
        {
            for (int i = 0; i < guidList.Count; i++)
            {
                var p = command.Parameters.Add($"@guid{i}", SqlDbType.UniqueIdentifier);
                p.Value = guidList[i];
            }
        }

        private async Task<List<Library>> ExecuteLibraryQueryAsync(string sql)
        {
            using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync();
            using SqlCommand command = CreateCommand(connection, sql);

            return await ExecuteLibraryReaderStrictAsync(command);
        }

        private async Task<List<Library>> ExecuteLibraryReaderStrictAsync(SqlCommand command)
        {
            const string methodName = nameof(ExecuteLibraryReaderStrictAsync);
            _logger.LogDebug("{Method} - Executing reader for command: {CommandText}", methodName, command.CommandText);

            var libraries = new List<Library>();

            try
            {
                using SqlDataReader reader = await command.ExecuteReaderAsync();

                int ordGuid = reader.GetOrdinal("Guid");
                int ordDepartmentId = reader.GetOrdinal("DepartmentId");
                int ordReadonly = reader.GetOrdinal("Readonly");
                int ordIsDefault = reader.GetOrdinal("IsDefault");

                int ordName = reader.GetOrdinal("Name");
                int ordSharingType = reader.GetOrdinal("SharingType");
                int ordDescription = reader.GetOrdinal("Description");
                int ordLabels = reader.GetOrdinal("Labels");
                int ordVersion = reader.GetOrdinal("Version");
                int ordReleaseNotes = reader.GetOrdinal("ReleaseNotes");
                int ordImageUrl = reader.GetOrdinal("ImageURL");

                while (await reader.ReadAsync())
                {
                    libraries.Add(new Library
                    {
                        Guid = reader.GetGuid(ordGuid),
                        DepartmentId = reader.GetInt32(ordDepartmentId),
                        Readonly = reader.GetBoolean(ordReadonly),
                        IsDefault = reader.GetBoolean(ordIsDefault),

                        // Use the same "safe" fill approach as ComponentRepository
                        Name = reader.GetValue(ordName).ToSafeString(),
                        SharingType = reader.GetValue(ordSharingType).ToSafeString(),
                        Description = reader.GetValue(ordDescription).ToSafeString(),
                        Version = reader.GetValue(ordVersion).ToSafeString(),
                        ReleaseNotes = reader.GetValue(ordReleaseNotes).ToSafeString(),
                        ImageURL = reader.GetValue(ordImageUrl).ToSafeString(),

                        // IMPORTANT: Library.Labels is List<string>, so parse it like Component.Labels
                        Labels = reader.GetValue(ordLabels).ToLabelList()
                    });
                }

                _logger.LogDebug("{Method} - Successfully mapped {Count} libraries.", methodName, libraries.Count);
                return libraries;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "{Method} - Error executing/mapping libraries. Command: {Sql}",
                    methodName,
                    command.CommandText);

                throw;
            }
        }
    }
}
