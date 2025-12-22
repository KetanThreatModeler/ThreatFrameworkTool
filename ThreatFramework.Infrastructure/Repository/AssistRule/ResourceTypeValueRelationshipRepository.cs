using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThreatFramework.Infra.Contract;
using ThreatModeler.TF.Core.Model.AssistRules;
using ThreatModeler.TF.Infra.Contract.Repository.AssistRules;

namespace ThreatModeler.TF.Infra.Implmentation.Repository.AssistRule
{
    public class ResourceTypeValueRelationshipRepository : IResourceTypeValueRelationshipRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;
        private readonly ILibraryCacheService _libraryCacheService;
        private readonly ILogger<ResourceTypeValueRelationshipRepository> _logger;

        public ResourceTypeValueRelationshipRepository(
            ISqlConnectionFactory connectionFactory,
            ILibraryCacheService libraryCacheService,
            ILogger<ResourceTypeValueRelationshipRepository> logger)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _libraryCacheService = libraryCacheService ?? throw new ArgumentNullException(nameof(libraryCacheService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<ResourceTypeValueRelationship>> GetAllAsync()
        {
            try
            {
                _logger.LogDebug("Executing GetAllAsync for ResourceTypeValueRelationships.");

                using var connection = await _connectionFactory.CreateOpenConnectionAsync();
                using var command = new SqlCommand(BuildSelectQuery(), connection);

                return await ExecuteReaderAsync(command);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching all ResourceTypeValueRelationships.");
                throw new Exception(
                    "Something went wrong while retrieving resource type value relationships.", ex);
            }
        }

        public async Task<IEnumerable<ResourceTypeValueRelationship>> GetByLibraryGuidsAsync(List<Guid> libraryGuids)
        {
            try
            {
                _logger.LogDebug(
                    "Executing GetByLibraryGuidsAsync for ResourceTypeValueRelationships. LibraryGuids: {LibraryGuids}",
                    libraryGuids);

                var libraryIds = await _libraryCacheService.GetIdsFromGuid(libraryGuids);
                if (!libraryIds.Any())
                    return Enumerable.Empty<ResourceTypeValueRelationship>();

                // NOTE: Keeping your existing pattern; if you want proper parameterization for IN,
                // we can convert this to individual parameters (@id0,@id1,...) safely.
                var sql = $"{BuildSelectQuery()} WHERE rtv.LIbraryId IN ({string.Join(",", libraryIds)})";

                using var connection = await _connectionFactory.CreateOpenConnectionAsync();
                using var command = new SqlCommand(sql, connection);

                return await ExecuteReaderAsync(command);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error occurred while fetching ResourceTypeValueRelationships by LibraryGuids {LibraryGuids}",
                    libraryGuids);

                throw new Exception(
                    "Something went wrong while retrieving resource type value relationships by library.", ex);
            }
        }

        public async Task<IEnumerable<ResourceTypeValueRelationship>> GetBySourceResourceTypeValueAsync(
            string sourceResourceTypeValue)
        {
            try
            {
                _logger.LogDebug(
                    "Executing GetBySourceResourceTypeValueAsync. SourceResourceTypeValue: {Value}",
                    sourceResourceTypeValue);

                var sql = $"{BuildSelectQuery()} WHERE rtv.SourceResourceTypeValue = @sourceValue";

                using var connection = await _connectionFactory.CreateOpenConnectionAsync();
                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@sourceValue", sourceResourceTypeValue);

                return await ExecuteReaderAsync(command);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error occurred while fetching ResourceTypeValueRelationships by SourceResourceTypeValue {Value}",
                    sourceResourceTypeValue);

                throw new Exception(
                    "Something went wrong while retrieving resource type value relationships by source value.", ex);
            }
        }

        private static string BuildSelectQuery()
        {
            // Added join to dbo.Relationships to populate RelationshipName
            return @"
                SELECT 
                    rtv.SourceResourceTypeValue,
                    rtv.RelationshipGuid,
                    rel.[Relationship] AS RelationshipName,
                    rtv.TargetResourceTypeValue,
                    rtv.IsRequired,
                    rtv.LIbraryId,
                    rtv.IsDeleted
                FROM [dbo].[ResporceTypeValueRelationships] rtv
                LEFT JOIN [dbo].[Relationships] rel
                    ON rel.[Guid] = rtv.RelationshipGuid";
        }

        private async Task<IEnumerable<ResourceTypeValueRelationship>> ExecuteReaderAsync(SqlCommand command)
        {
            var results = new List<ResourceTypeValueRelationship>();

            using var reader = await command.ExecuteReaderAsync();

            var sourceOrdinal = reader.GetOrdinal("SourceResourceTypeValue");
            var relationshipGuidOrdinal = reader.GetOrdinal("RelationshipGuid");
            var relationshipNameOrdinal = reader.GetOrdinal("RelationshipName"); // NEW
            var targetOrdinal = reader.GetOrdinal("TargetResourceTypeValue");
            var isRequiredOrdinal = reader.GetOrdinal("IsRequired");
            var libraryIdOrdinal = reader.GetOrdinal("LIbraryId");
            var isDeletedOrdinal = reader.GetOrdinal("IsDeleted");

            while (await reader.ReadAsync())
            {
                results.Add(new ResourceTypeValueRelationship
                {
                    SourceResourceTypeValue = reader.IsDBNull(sourceOrdinal)
                        ? null
                        : reader.GetString(sourceOrdinal),

                    RelationshipGuid = reader.GetGuid(relationshipGuidOrdinal),

                    RelationshipName = reader.IsDBNull(relationshipNameOrdinal)
                        ? string.Empty
                        : reader.GetString(relationshipNameOrdinal),

                    TargetResourceTypeValue = reader.IsDBNull(targetOrdinal)
                        ? null
                        : reader.GetString(targetOrdinal),

                    IsRequired = reader.GetBoolean(isRequiredOrdinal),

                    LibraryId = await _libraryCacheService.GetGuidByIdAsync(
                        reader.GetInt32(libraryIdOrdinal)),

                    IsDeleted = reader.GetBoolean(isDeletedOrdinal)
                });
            }

            return results;
        }
    }
}
