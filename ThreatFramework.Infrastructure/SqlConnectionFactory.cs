using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatFramework.Infrastructure
{
    public class SqlConnectionFactory : ISqlConnectionFactory
    {
        private readonly string _connectionString;

        public SqlConnectionFactory(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found in configuration.");
        }

        public async Task<SqlConnection> CreateOpenConnectionAsync(CancellationToken ct = default)
        {
            var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(ct);
            return connection;
        }
    }
}
