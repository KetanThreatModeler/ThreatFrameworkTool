using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;
using ThreatFramework.Infra.Contract;
using ThreatModeler.TF.Core.Model.CoreEntities;
using ThreatModeler.TF.Infra.Contract.Repository.CoreEntities;
using ThreatModeler.TF.Infra.Implmentation.Helper;

namespace ThreatModeler.TF.Infra.Implmentation.Repository.CoreEntities
{
    public class ComponentRepository : IComponentRepository
    {
        private readonly ILibraryCacheService _libraryCacheService;
        private readonly ISqlConnectionFactory _connectionFactory;
        private readonly ILogger<ComponentRepository> _logger;

        // Optional: adjust if you want
        private const int DefaultCommandTimeoutSeconds = 30;

        public ComponentRepository(
            ILibraryCacheService libraryCacheService,
            ISqlConnectionFactory sqlConnectionFactory,
            ILogger<ComponentRepository> logger)
        {
            _libraryCacheService = libraryCacheService ?? throw new ArgumentNullException(nameof(libraryCacheService));
            _connectionFactory = sqlConnectionFactory ?? throw new ArgumentNullException(nameof(sqlConnectionFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<Component>> GetReadOnlyComponentsAsync()
        {
            const string methodName = nameof(GetReadOnlyComponentsAsync);
            _logger.LogInformation("{Method} - Starting execution.", methodName);

            HashSet<int> readonlyLibraryIds = await _libraryCacheService.GetReadOnlyLibraryIdAsync();

            if (readonlyLibraryIds == null || readonlyLibraryIds.Count == 0)
            {
                _logger.LogInformation("{Method} - No read-only libraries found. Returning empty list.", methodName);
                return Enumerable.Empty<Component>();
            }

            List<int> libraryIdList = readonlyLibraryIds.ToList();
            string libraryParameters = string.Join(",", libraryIdList.Select((_, i) => $"@lib{i}"));

            string sql = $@"{BuildComponentSelectQuery()}
                            WHERE c.LibraryId IN ({libraryParameters})";

            _logger.LogDebug("{Method} - Executing SQL with LibraryIds: {LibraryIds}. SQL: {Sql}",
                methodName, string.Join(",", libraryIdList), sql);

            using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync();
            using SqlCommand command = CreateCommand(connection, sql);

            AddLibraryIdParameters(command, libraryIdList);

            var result = await ExecuteComponentReaderStrictAsync(command);
            _logger.LogInformation("{Method} - Successfully retrieved {Count} read-only components.",
                methodName, result.Count);

            return result;
        }

        public async Task<IEnumerable<Component>> GetComponentsByLibraryIdAsync(IEnumerable<Guid> libraryIds)
        {
            const string methodName = nameof(GetComponentsByLibraryIdAsync);

            if (libraryIds == null)
                throw new ArgumentNullException(nameof(libraryIds));

            var libraryGuidList = libraryIds as IList<Guid> ?? libraryIds.ToList();
            _logger.LogInformation("{Method} - Starting for {Count} library GUIDs.", methodName, libraryGuidList.Count);

            HashSet<int> ids = await _libraryCacheService.GetIdsFromGuid(libraryGuidList);

            if (ids == null || ids.Count == 0)
            {
                _logger.LogWarning("{Method} - No matching Library IDs found for the provided GUIDs.", methodName);
                return Enumerable.Empty<Component>();
            }

            List<int> libraryIdList = ids.ToList();
            string libraryParameters = string.Join(",", libraryIdList.Select((_, i) => $"@lib{i}"));

            string sql = $@"{BuildComponentSelectQuery()}
                            WHERE c.LibraryId IN ({libraryParameters})";

            _logger.LogDebug("{Method} - Executing SQL with LibraryIds: {LibraryIds}. SQL: {Sql}",
                methodName, string.Join(",", libraryIdList), sql);

            using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync();
            using SqlCommand command = CreateCommand(connection, sql);

            AddLibraryIdParameters(command, libraryIdList);

            var result = await ExecuteComponentReaderStrictAsync(command);
            _logger.LogInformation("{Method} - Successfully retrieved {Count} components by Library IDs.",
                methodName, result.Count);

            return result;
        }

        public async Task<IEnumerable<Guid>> GetGuidsAsync()
        {
            const string methodName = nameof(GetGuidsAsync);
            const string sql = "SELECT [Guid] FROM [Components];";

            _logger.LogInformation("{Method} - Starting execution.", methodName);

            using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync();
            using SqlCommand command = CreateCommand(connection, sql);

            var guids = new List<Guid>();

            using SqlDataReader reader = await command.ExecuteReaderAsync();
            int ordGuid = reader.GetOrdinal("Guid");

            while (await reader.ReadAsync())
            {
                guids.Add(reader.GetGuid(ordGuid));
            }

            _logger.LogInformation("{Method} - Successfully retrieved {Count} component GUIDs.", methodName, guids.Count);
            return guids;
        }

        public async Task<IEnumerable<(Guid ComponentGuid, Guid LibraryGuid)>> GetGuidsAndLibraryGuidsAsync()
        {
            const string methodName = "GetGuidsAndLibraryGuidsAsync_NoFilter";
            const string sql = @"
                SELECT c.Guid AS ComponentGuid, l.Guid AS LibraryGuid
                FROM Components c
                INNER JOIN Libraries l ON c.LibraryId = l.Id;";

            _logger.LogInformation("{Method} - Starting execution (no library filter).", methodName);

            using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync();
            using SqlCommand command = CreateCommand(connection, sql);

            var results = new List<(Guid ComponentGuid, Guid LibraryGuid)>();

            using SqlDataReader reader = await command.ExecuteReaderAsync();
            int ordComponentGuid = reader.GetOrdinal("ComponentGuid");
            int ordLibraryGuid = reader.GetOrdinal("LibraryGuid");

            while (await reader.ReadAsync())
            {
                results.Add((
                    reader.GetGuid(ordComponentGuid),
                    reader.GetGuid(ordLibraryGuid)
                ));
            }

            _logger.LogInformation("{Method} - Successfully retrieved {Count} Component/Library GUID pairs.",
                methodName, results.Count);

            return results;
        }

        public async Task<IEnumerable<Guid>> GetGuidsByLibraryIds(IEnumerable<Guid> libraryIds)
        {
            const string methodName = nameof(GetGuidsByLibraryIds);

            if (libraryIds == null)
                throw new ArgumentNullException(nameof(libraryIds));

            var libraryGuidList = libraryIds as IList<Guid> ?? libraryIds.ToList();
            _logger.LogInformation("{Method} - Starting for {Count} library GUIDs.", methodName, libraryGuidList.Count);

            if (libraryGuidList.Count == 0)
            {
                _logger.LogInformation("{Method} - No library GUIDs provided. Returning empty result.", methodName);
                return Enumerable.Empty<Guid>();
            }

            HashSet<int> ids = await _libraryCacheService.GetIdsFromGuid(libraryGuidList);

            if (ids == null || ids.Count == 0)
            {
                _logger.LogWarning("{Method} - No matching Library IDs found for the provided GUIDs.", methodName);
                return Enumerable.Empty<Guid>();
            }

            List<int> libraryIdList = ids.ToList();
            string libraryParameters = string.Join(",", libraryIdList.Select((_, i) => $"@lib{i}"));

            string sql = $@"SELECT [Guid]
                            FROM [Components]
                            WHERE [LibraryId] IN ({libraryParameters});";

            _logger.LogDebug("{Method} - Executing SQL with LibraryIds: {LibraryIds}. SQL: {Sql}",
                methodName, string.Join(",", libraryIdList), sql);

            using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync();
            using SqlCommand command = CreateCommand(connection, sql);

            AddLibraryIdParameters(command, libraryIdList);

            var guids = new List<Guid>();

            using SqlDataReader reader = await command.ExecuteReaderAsync();
            int ordGuid = reader.GetOrdinal("Guid");

            while (await reader.ReadAsync())
            {
                guids.Add(reader.GetGuid(ordGuid));
            }

            _logger.LogInformation("{Method} - Successfully retrieved {Count} Component GUIDs.",
                methodName, guids.Count);

            return guids;
        }

        public async Task<IEnumerable<(Guid ComponentGuid, Guid LibraryGuid)>> GetGuidsAndLibraryGuidsAsync(IEnumerable<Guid> libraryIds)
        {
            const string methodName = nameof(GetGuidsAndLibraryGuidsAsync);

            if (libraryIds == null)
                throw new ArgumentNullException(nameof(libraryIds));

            var libraryGuidList = libraryIds as IList<Guid> ?? libraryIds.ToList();
            _logger.LogInformation("{Method} - Starting for {Count} library GUIDs.", methodName, libraryGuidList.Count);

            if (libraryGuidList.Count == 0)
            {
                _logger.LogInformation("{Method} - No library GUIDs provided. Returning empty result.", methodName);
                return Enumerable.Empty<(Guid ComponentGuid, Guid LibraryGuid)>();
            }

            HashSet<int> ids = await _libraryCacheService.GetIdsFromGuid(libraryGuidList);

            if (ids == null || ids.Count == 0)
            {
                _logger.LogWarning("{Method} - No matching Library IDs found for the provided GUIDs.", methodName);
                return Enumerable.Empty<(Guid ComponentGuid, Guid LibraryGuid)>();
            }

            List<int> libraryIdList = ids.ToList();
            string libraryParameters = string.Join(",", libraryIdList.Select((_, i) => $"@lib{i}"));

            string sql = $@"
                SELECT c.Guid AS ComponentGuid, l.Guid AS LibraryGuid
                FROM Components c
                INNER JOIN Libraries l ON c.LibraryId = l.Id
                WHERE c.LibraryId IN ({libraryParameters});";

            _logger.LogDebug("{Method} - Executing SQL with LibraryIds: {LibraryIds}. SQL: {Sql}",
                methodName, string.Join(",", libraryIdList), sql);

            using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync();
            using SqlCommand command = CreateCommand(connection, sql);

            AddLibraryIdParameters(command, libraryIdList);

            var results = new List<(Guid ComponentGuid, Guid LibraryGuid)>();

            using SqlDataReader reader = await command.ExecuteReaderAsync();
            int ordComponentGuid = reader.GetOrdinal("ComponentGuid");
            int ordLibraryGuid = reader.GetOrdinal("LibraryGuid");

            while (await reader.ReadAsync())
            {
                results.Add((
                    reader.GetGuid(ordComponentGuid),
                    reader.GetGuid(ordLibraryGuid)
                ));
            }

            _logger.LogInformation("{Method} - Successfully retrieved {Count} Component/Library GUID pairs for specified libraries.",
                methodName, results.Count);

            return results;
        }

        // --------------------------
        // Private helpers (STRICT)
        // --------------------------

        private static string BuildComponentSelectQuery()
        {
            return @"
        SELECT
            c.Id,
            c.Guid,
            c.LibraryId,
            c.ComponentTypeId,
            ct.Guid AS ComponentTypeGuid,
            c.[isHidden] AS IsHidden,
            c.LastUpdated,
            c.Name,
            c.ImagePath,
            c.Labels,
            c.Version,
            c.Description,
            c.ChineseDescription
        FROM Components c
        INNER JOIN ComponentTypes ct ON c.ComponentTypeId = ct.Id";
        }


        private SqlCommand CreateCommand(SqlConnection connection, string sql)
        {
            var cmd = new SqlCommand(sql, connection)
            {
                CommandType = CommandType.Text,
                CommandTimeout = DefaultCommandTimeoutSeconds
            };
            return cmd;
        }

        private static void AddLibraryIdParameters(SqlCommand command, List<int> libraryIdList)
        {
            for (int i = 0; i < libraryIdList.Count; i++)
            {
                // Avoid AddWithValue
                var p = command.Parameters.Add($"@lib{i}", SqlDbType.Int);
                p.Value = libraryIdList[i];
            }
        }


        private async Task<List<Component>> ExecuteComponentReaderStrictAsync(SqlCommand command)
        {
            const string methodName = nameof(ExecuteComponentReaderStrictAsync);

            _logger.LogDebug("{Method} - Executing reader for command: {CommandText}", methodName, command.CommandText);

            var components = new List<Component>();

            try
            {
                using SqlDataReader reader = await command.ExecuteReaderAsync();

                int ordGuid = reader.GetOrdinal("Guid");
                int ordLibraryId = reader.GetOrdinal("LibraryId");
                int ordComponentTypeGuid = reader.GetOrdinal("ComponentTypeGuid");
                int ordIsHidden = reader.GetOrdinal("IsHidden"); // mapped via alias
                int ordName = reader.GetOrdinal("Name");
                int ordImagePath = reader.GetOrdinal("ImagePath");
                int ordLabels = reader.GetOrdinal("Labels");
                int ordVersion = reader.GetOrdinal("Version");
                int ordDescription = reader.GetOrdinal("Description");
                int ordChineseDescription = reader.GetOrdinal("ChineseDescription");

                while (await reader.ReadAsync())
                {
                    var libraryId = reader.GetInt32(ordLibraryId);
                    Guid libraryGuid = await _libraryCacheService.GetGuidByIdAsync(libraryId);

                    var component = new Component
                    {
                        Guid = reader.GetGuid(ordGuid),
                        LibraryGuid = libraryGuid,
                        ComponentTypeGuid = reader.GetGuid(ordComponentTypeGuid),

                        IsHidden = reader.GetBoolean(ordIsHidden),

                        // Column not present in your table script -> default to false
                        IsOverridden = false,

                        Name = reader.GetValue(ordName).ToSafeString(),
                        ImagePath = reader.GetValue(ordImagePath).ToSafeString(),
                        Version = reader.GetValue(ordVersion).ToSafeString(),
                        Description = reader.GetValue(ordDescription).ToSafeString(),
                        ChineseDescription = reader.GetValue(ordChineseDescription).ToSafeString(),

                        Labels = reader.GetValue(ordLabels).ToLabelList()
                    };

                    components.Add(component);
                }

                _logger.LogDebug("{Method} - Successfully mapped {Count} components.", methodName, components.Count);
                return components;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "{Method} - Error executing/mapping components. Command: {Sql}",
                    methodName,
                    command.CommandText);

                throw;
            }
        }

    }
}
