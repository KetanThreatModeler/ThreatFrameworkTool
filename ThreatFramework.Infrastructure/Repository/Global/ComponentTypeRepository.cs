using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using ThreatFramework.Infra.Contract;
using ThreatModeler.TF.Core.Model.Global;
using ThreatModeler.TF.Infra.Contract.Repository.Global;

namespace ThreatModeler.TF.Infra.Implmentation.Repository.Global
{
    public class ComponentTypeRepository : IComponentTypeRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;
        private readonly ILibraryCacheService _libraryCacheService;
        private readonly ILogger<ComponentTypeRepository> _logger;

        // SQL Column Order:
        // 0:[Id], 1:[Name], 2:[Description], 3:[LibraryId], 4:[Guid], 
        // 5:[isHidden], 6:[IsSecurityControl], 7:[ChineseName], 8:[ChineseDescription]
        private const string SqlGetAll = @"
            SELECT [Id], [Name], [Description], [LibraryId], [Guid], 
                   [isHidden], [IsSecurityControl], [ChineseName], [ChineseDescription]
            FROM [dbo].[ComponentTypes];";

        private const string SqlGetGuids = @"
            SELECT ct.Guid AS ComponentTypeGuid
            FROM ComponentTypes ct
            INNER JOIN Libraries l ON ct.LibraryId = l.Id;";

        public ComponentTypeRepository(
            ILibraryCacheService libraryCacheService,
            ISqlConnectionFactory connectionFactory,
            ILogger<ComponentTypeRepository> logger)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _libraryCacheService = libraryCacheService ?? throw new ArgumentNullException(nameof(libraryCacheService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<ComponentType>> GetComponentTypesAsync()
        {
            using (_logger.BeginScope("Operation: GetComponentTypes"))
            {
                return await ExecuteQueryAsync(SqlGetAll, async reader =>
                {
                    // STRICT SEQUENTIAL READ ORDER (1 -> 8)
                    // We skip [Id] (Index 0) as it is not mapped to the model.

                    // 1. Read Name (Index 1)
                    string name = GetStringSafe(reader, "Name");

                    // 2. Read Description (Index 2)
                    string description = GetStringSafe(reader, "Description");

                    // 3. Read LibraryId (Index 3)
                    int libraryId = reader.GetInt32(reader.GetOrdinal("LibraryId"));

                    // 4. Read Guid (Index 4)
                    Guid guid = reader.GetGuid(reader.GetOrdinal("Guid"));

                    // 5. Read isHidden (Index 5)
                    bool isHidden = reader.GetBoolean(reader.GetOrdinal("isHidden"));

                    // 6. Read IsSecurityControl (Index 6)
                    bool isSecurityControl = reader.GetBoolean(reader.GetOrdinal("IsSecurityControl"));

                    // 7. Read ChineseName (Index 7)
                    string chineseName = GetStringSafe(reader, "ChineseName");

                    // 8. Read ChineseDescription (Index 8)
                    string chineseDescription = GetStringSafe(reader, "ChineseDescription");

                    // ---------------------------------------------------------
                    // DATA READING COMPLETE. NOW SAFE TO DO LOGIC/ASYNC CALLS.
                    // ---------------------------------------------------------

                    // Resolve Library Guid using the ID we read earlier
                    Guid libraryGuid = await _libraryCacheService.GetGuidByIdAsync(libraryId);

                    return new ComponentType
                    {
                        Guid = guid,
                        Name = name,
                        Description = description,
                        LibraryGuid = libraryGuid,
                        IsHidden = isHidden,
                        IsSecurityControl = isSecurityControl,
                        ChineseName = chineseName,
                        ChineseDescription = chineseDescription
                    };
                });
            }
        }

        public async Task<IEnumerable<Guid>> GetGuidsAndLibraryGuidsAsync()
        {
            using (_logger.BeginScope("Operation: GetComponentTypeGuids"))
            {
                return await ExecuteQueryAsync(SqlGetGuids, reader =>
                {
                    return Task.FromResult(reader.GetGuid(reader.GetOrdinal("ComponentTypeGuid")));
                });
            }
        }

        // --------------------------------------------------------------------------------
        // DRY Helper: Generic ADO.NET Executor
        // --------------------------------------------------------------------------------
        private async Task<IEnumerable<T>> ExecuteQueryAsync<T>(string sql, Func<SqlDataReader, Task<T>> mapFunc)
        {
            var results = new List<T>();

            try
            {
                _logger.LogDebug("Executing SQL Query: {Sql}", sql);

                using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync();
                using SqlCommand command = new SqlCommand(sql, connection);

                // SequentialAccess improves performance by reading data as a stream
                using SqlDataReader reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess);

                while (await reader.ReadAsync())
                {
                    results.Add(await mapFunc(reader));
                }

                _logger.LogInformation("Query executed successfully. Fetched {Count} records.", results.Count);
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database operation failed.");
                throw;
            }
        }

        // --------------------------------------------------------------------------------
        // Helper: Null-Safe String Retrieval
        // --------------------------------------------------------------------------------
        private static string GetStringSafe(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal);
        }
    }
}