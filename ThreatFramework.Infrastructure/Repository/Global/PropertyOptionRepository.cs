using Microsoft.Data.SqlClient;
using ThreatFramework.Infra.Contract;
using ThreatModeler.TF.Core.Global;
using ThreatModeler.TF.Infra.Contract.Repository.Global;

namespace ThreatModeler.TF.Infra.Implmentation.Repository.Global
{
    public class PropertyOptionRepository : IPropertyOptionRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public PropertyOptionRepository(ISqlConnectionFactory sqlConnectionFactory)
        {
            _connectionFactory = sqlConnectionFactory;
        }

        public async Task<IEnumerable<Guid>> GetAllPropertyOptionGuidsAsync()
        {
            var sql = "SELECT Guid FROM PropertyOptions";

            using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            using var command = new SqlCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();

            var guids = new List<Guid>();
            while (await reader.ReadAsync())
            {
                guids.Add((Guid)reader["Guid"]);
            }

            return guids;
        }

        public async Task<IEnumerable<PropertyOption>> GetAllPropertyOptionsAsync()
        {
            var sql = @"SELECT Id, PropertyId, IsDefault, isHidden, IsOverridden, CreatedDate, 
                               LastUpdated, Guid, OptionText, ChineseOptionText 
                        FROM PropertyOptions";

            using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            using var command = new SqlCommand(sql, connection);

            return await ExecutePropertyOptionReaderAsync(command);
        }

        private async Task<IEnumerable<PropertyOption>> ExecutePropertyOptionReaderAsync(SqlCommand command)
        {
            var propertyOptions = new List<PropertyOption>();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                propertyOptions.Add(new PropertyOption
                {
                    Id = (int)reader["Id"],
                    PropertyId = reader["PropertyId"] as int?,
                    IsDefault = (bool)reader["IsDefault"],
                    IsHidden = (bool)reader["isHidden"],
                    IsOverridden = (bool)reader["IsOverridden"],
                    Guid = (Guid)reader["Guid"],
                    OptionText = reader["OptionText"] as string,
                    ChineseOptionText = reader["ChineseOptionText"] as string
                });
            }

            return propertyOptions;
        }
    }
}
