using Microsoft.Data.SqlClient;
using ThreatModeler.TF.Core.Model.Global;
using ThreatModeler.TF.Infra.Contract.Repository.Global;

namespace ThreatModeler.TF.Infra.Implmentation.Repository.Global
{
    public class RiskRepository : IRiskRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public RiskRepository(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory
                ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        public async Task<IEnumerable<Risk>> GetAllRisksAsync()
        {
            const string sql = @"
                SELECT 
                    [Name],
                    [Color],
                    [SuggestedName],
                    [Score],
                    [ChineseName]
                FROM [dbo].[Risks];";

            List<Risk> results = new();

            using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync().ConfigureAwait(false);
            using SqlCommand command = new(sql, connection);
            using SqlDataReader reader = await command.ExecuteReaderAsync().ConfigureAwait(false);

            int nameOrdinal = reader.GetOrdinal("Name");
            int colorOrdinal = reader.GetOrdinal("Color");
            int suggestedNameOrdinal = reader.GetOrdinal("SuggestedName");
            int scoreOrdinal = reader.GetOrdinal("Score");
            int chineseNameOrdinal = reader.GetOrdinal("ChineseName");

            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                Risk risk = new()
                {
                    Name = reader.IsDBNull(nameOrdinal)
                        ? string.Empty
                        : reader.GetString(nameOrdinal),

                    Color = reader.IsDBNull(colorOrdinal)
                        ? string.Empty // required, fallback if DB has invalid null data
                        : reader.GetString(colorOrdinal),

                    SuggestedName = reader.IsDBNull(suggestedNameOrdinal)
                        ? string.Empty // required, fallback
                        : reader.GetString(suggestedNameOrdinal),

                    Score = reader.IsDBNull(scoreOrdinal)
                        ? 0
                        : reader.GetInt32(scoreOrdinal),

                    ChineseName = reader.IsDBNull(chineseNameOrdinal)
                        ? string.Empty // required, fallback
                        : reader.GetString(chineseNameOrdinal)
                };

                results.Add(risk);
            }

            return results;
        }
    }
}
