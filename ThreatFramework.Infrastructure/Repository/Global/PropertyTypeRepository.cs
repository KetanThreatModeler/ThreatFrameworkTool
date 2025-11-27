using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatModeler.TF.Core.Global;
using ThreatModeler.TF.Infra.Contract.Repository.Global;

namespace ThreatModeler.TF.Infra.Implmentation.Repository.Global
{
    public class PropertyTypeRepository : IPropertyTypeRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public PropertyTypeRepository(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        public async Task<IEnumerable<PropertyType>> GetAllPropertyTypeAsync()
        {
            const string sql = @"
            SELECT [Guid], [Name]
            FROM [dbo].[PropertyTypes];";

            var results = new List<PropertyType>();

            using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            using var command = new SqlCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();

            var guidOrdinal = reader.GetOrdinal("Guid");
            var nameOrdinal = reader.GetOrdinal("Name");

            while (await reader.ReadAsync())
            {
                results.Add(new PropertyType
                {
                    Guid = reader.GetGuid(guidOrdinal),
                    Name = reader.IsDBNull(nameOrdinal) ? string.Empty : reader.GetString(nameOrdinal)
                });
            }

            return results;
        }

        public async Task<IEnumerable<Guid>> GetAllPropertyTypeGuidsAsync()
        {
            const string sql = @"
            SELECT [Guid]
            FROM [dbo].[PropertyTypes];";

            var results = new List<Guid>();

            using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            using var command = new SqlCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();

            var guidOrdinal = reader.GetOrdinal("Guid");

            while (await reader.ReadAsync())
            {
                results.Add(reader.GetGuid(guidOrdinal));
            }
            return results;
        }
    }
}