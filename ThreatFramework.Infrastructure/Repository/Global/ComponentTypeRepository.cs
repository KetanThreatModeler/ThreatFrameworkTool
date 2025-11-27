using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using ThreatFramework.Infra.Contract;
using ThreatModeler.TF.Core.Global;
using ThreatModeler.TF.Infra.Contract.Repository.Global;

namespace ThreatModeler.TF.Infra.Implmentation.Repository.Global
{
    public class ComponentTypeRepository : IComponentTypeRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;
        private readonly ILibraryCacheService _libraryCacheService;
        private readonly ILogger<ComponentTypeRepository> _logger;

        // SQL Constants to keep the code clean
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
                    // Map LibraryId (int) to LibraryGuid using the Cache Service
                    int libraryId = reader.GetInt32(reader.GetOrdinal("LibraryId"));
                    Guid libraryGuid = await _libraryCacheService.GetGuidByIdAsync(libraryId);

                    return new ComponentType
                    {
                        Guid = reader.GetGuid(reader.GetOrdinal("Guid")),
                        Name = GetStringSafe(reader, "Name"),
                        Description = GetStringSafe(reader, "Description"),
                        LibraryGuid = libraryGuid,
                        IsHidden = reader.GetBoolean(reader.GetOrdinal("isHidden")),
                        IsSecurityControl = reader.GetBoolean(reader.GetOrdinal("IsSecurityControl")),
                        ChineseName = GetStringSafe(reader, "ChineseName"),
                        ChineseDescription = GetStringSafe(reader, "ChineseDescription")
                    };
                });
            }
        }

        public async Task<IEnumerable<Guid>> GetGuidsAndLibraryGuidsAsync()
        {
            using (_logger.BeginScope("Operation: GetComponentTypeGuids"))
            {
                // Note: The original request signature returns IEnumerable<Guid>, 
                // so we only map the ComponentTypeGuid.
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

                // SequentialAccess provides better performance for reading rows
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