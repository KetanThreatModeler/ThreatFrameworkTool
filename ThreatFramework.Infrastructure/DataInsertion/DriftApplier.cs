using Microsoft.Data.SqlClient;
using System.Data;
using System.Data.Common;
using System.Text.Json;
using System.Text.Json.Serialization;
using ThreatFramework.Drift.Contract.Model;
using ThreatFramework.Infra.Contract.DataInsertion;

namespace ThreatFramework.Infrastructure.DataInsertion
{
    public sealed class DriftApplier : IDriftApplier
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        // one-time SP existence check
        private volatile bool _procChecked;
        private readonly object _procCheckLock = new();

        private const string ProcSchema = "dbo";
        private const string ProcName = "ApplyTMFrameworkDrift";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            // Keep PascalCase to match $.AddedLibraries, $.ModifiedLibraries...
            PropertyNamingPolicy = null,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };

        public DriftApplier(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task ApplyAsync(TMFrameworkDrift drift)
        {
            if (drift is null)
            {
                throw new ArgumentNullException(nameof(drift));
            }

            string json = JsonSerializer.Serialize(drift, JsonOptions);

            Console.WriteLine(json);
            await using DbConnection conn = await _connectionFactory.CreateOpenConnectionAsync()
                                                                    .ConfigureAwait(false);

            await EnsureStoredProcedureExistsAsync(conn).ConfigureAwait(false);



            await using (var cmdDb = new SqlCommand("SELECT DB_NAME()", (SqlConnection)conn))
            {
                var db = (string?)await cmdDb.ExecuteScalarAsync();
                Console.WriteLine($"ApplyTMFrameworkDrift executing against DB: {db}");
            }


            await using SqlCommand cmd = new($"{ProcSchema}.{ProcName}", (SqlConnection)conn)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 4000 // adjust for very large payloads
            };

            _ = cmd.Parameters.Add(new SqlParameter("@DriftJson", SqlDbType.NVarChar, -1) { Value = json });

            try
            {
                Console.WriteLine($"Executing {ProcSchema}.{ProcName} on ClientDb (payload length: {json.Length})");

                _ = await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);

                Console.WriteLine($"{ProcSchema}.{ProcName} completed successfully.");
            }
            catch (SqlException) // "Could not find stored procedure"
            {
                string msg = $"Stored procedure {ProcSchema}.{ProcName} not found on ClientDb.";
                Console.WriteLine($"ERROR: {msg}");
                throw;
            }
        }

        private async Task EnsureStoredProcedureExistsAsync(DbConnection conn, CancellationToken cancellationToken = default)
        {
            if (_procChecked)
            {
                return;
            }

            lock (_procCheckLock)
            {
                if (_procChecked)
                {
                    return;
                }
            }

            const string sql = @"
SELECT 1
FROM sys.procedures p
JOIN sys.schemas s ON s.schema_id = p.schema_id
WHERE p.name = @ProcName AND s.name = @SchemaName;";

            await using SqlCommand cmd = new(sql, (SqlConnection)conn);
            _ = cmd.Parameters.Add(new SqlParameter("@ProcName", SqlDbType.NVarChar, 128) { Value = ProcName });
            _ = cmd.Parameters.Add(new SqlParameter("@SchemaName", SqlDbType.NVarChar, 128) { Value = ProcSchema });

            object? result = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

            if (result is null)
            {
                string msg = $"Stored procedure {ProcSchema}.{ProcName} does not exist on ClientDb.";
                Console.WriteLine($"ERROR: {msg}");
                throw new InvalidOperationException(msg);
            }

            lock (_procCheckLock)
            {
                _procChecked = true;
            }
        }
    }
}
