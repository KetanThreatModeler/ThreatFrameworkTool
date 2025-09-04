using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

public interface ISqlConnectionFactory
{
    Task<SqlConnection> CreateOpenConnectionAsync(CancellationToken ct = default);
}