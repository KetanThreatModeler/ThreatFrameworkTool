using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ThreatFramework.Infra.Contract;
using ThreatModeler.TF.Core.Model.AssistRules;
using ThreatModeler.TF.Infra.Contract.Repository.AssistRules;

namespace ThreatModeler.TF.Infra.Implmentation.Repository.AssistRule
{
    public class RelationshipRepository : IRelationshipRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;
        private readonly ILogger<RelationshipRepository> _logger;

        public RelationshipRepository(
            ISqlConnectionFactory connectionFactory,
            ILogger<RelationshipRepository> logger)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<Guid>> GetAllGuidsAsync()
        {
            const string sql = @"SELECT [Guid] FROM [dbo].[Relationships]";

            try
            {
                _logger.LogDebug("Executing GetAllGuidsAsync for Relationships.");

                using var connection = await _connectionFactory.CreateOpenConnectionAsync();
                using var command = new SqlCommand(sql, connection);

                return await ExecuteGuidReaderAsync(command);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching Relationship GUIDs.");
                throw new Exception(
                    "Something went wrong while retrieving relationship GUIDs.", ex);
            }
        }

        public async Task<IEnumerable<Relationship>> GetAllRelationshipsAsync()
        {
            try
            {
                _logger.LogDebug("Executing GetAllRelationshipsAsync.");

                var sql = BuildRelationshipSelectQuery();

                using var connection = await _connectionFactory.CreateOpenConnectionAsync();
                using var command = new SqlCommand(sql, connection);

                return await ExecuteRelationshipReaderAsync(command);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching Relationships.");
                throw new Exception(
                    "Something went wrong while retrieving relationships.", ex);
            }
        }

        private static string BuildRelationshipSelectQuery()
        {
            return @"
                SELECT 
                    [Relationship],
                    [Description],
                    [Guid],
                    [ChineseRelationship]
                FROM [dbo].[Relationships]";
        }

        private static async Task<IEnumerable<Guid>> ExecuteGuidReaderAsync(SqlCommand command)
        {
            var guids = new List<Guid>();

            using var reader = await command.ExecuteReaderAsync();
            var guidOrdinal = reader.GetOrdinal("Guid");

            while (await reader.ReadAsync())
            {
                guids.Add(reader.GetGuid(guidOrdinal));
            }

            return guids;
        }

        private static async Task<IEnumerable<Relationship>> ExecuteRelationshipReaderAsync(SqlCommand command)
        {
            var relationships = new List<Relationship>();

            using var reader = await command.ExecuteReaderAsync();

            var relationshipOrdinal = reader.GetOrdinal("Relationship");
            var descriptionOrdinal = reader.GetOrdinal("Description");
            var guidOrdinal = reader.GetOrdinal("Guid");
            var chineseRelationshipOrdinal = reader.GetOrdinal("ChineseRelationship");

            while (await reader.ReadAsync())
            {
                relationships.Add(new Relationship
                {
                    RelationshipName = reader.IsDBNull(relationshipOrdinal)
                        ? null
                        : reader.GetString(relationshipOrdinal),

                    Description = reader.IsDBNull(descriptionOrdinal)
                        ? null
                        : reader.GetString(descriptionOrdinal),

                    Guid = reader.GetGuid(guidOrdinal),

                    ChineseRelationship = reader.IsDBNull(chineseRelationshipOrdinal)
                        ? null
                        : reader.GetString(chineseRelationshipOrdinal)
                });
            }

            return relationships;
        }
    }
}
