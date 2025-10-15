using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using ThreatFramework.Core.ComponentMapping;
using ThreatFramework.Infra.Contract;
using ThreatFramework.Infra.Contract.Repository;

namespace ThreatFramework.Infrastructure.Repository
{
    public class ComponentSecurityRequirementMappingRepository : IComponentSecurityRequirementMappingRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;
        private readonly ILibraryCacheService _libraryCacheService;

        public ComponentSecurityRequirementMappingRepository(
            ISqlConnectionFactory connectionFactory,
            ILibraryCacheService libraryCacheService)
        {
            _connectionFactory = connectionFactory;
            _libraryCacheService = libraryCacheService;
        }

      

        public async Task<IEnumerable<ComponentSecurityRequirementMapping>> GetMappingsByLibraryIdAsync(IEnumerable<Guid> libraryGuids)
        {

            try
            {
                var libraryIds = await _libraryCacheService.GetIdsFromGuid(libraryGuids);

                if (!libraryIds.Any())
                {
                    return Enumerable.Empty<ComponentSecurityRequirementMapping>();
                }

                var libraryIdList = libraryIds.ToList();
                var libraryParameters = string.Join(",", libraryIdList.Select((_, i) => $"@lib{i}"));

                var baseQuery = BuildMappingSelectQuery();
                var sql = $@"{baseQuery} 
                            WHERE (sr.LibraryId IN ({libraryParameters}) OR c.LibraryId IN ({libraryParameters}))";


                using var connection = await _connectionFactory.CreateOpenConnectionAsync();
                
                using var command = new SqlCommand(sql, connection);

                for (int i = 0; i < libraryIdList.Count; i++)
                {
                    command.Parameters.AddWithValue($"@lib{i}", libraryIdList[i]);
                }

                var result = await ExecuteMappingReaderAsync(command);
                return result;
            }
            catch (SqlException sqlEx)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<IEnumerable<ComponentSecurityRequirementMapping>> GetReadOnlyMappingsAsync()
        {
            try
            {
                var readonlyLibraryIds = await _libraryCacheService.GetReadOnlyLibraryIdAsync();

                if (!readonlyLibraryIds.Any())
                {
                    return Enumerable.Empty<ComponentSecurityRequirementMapping>();
                }

                var libraryIdList = readonlyLibraryIds.ToList();
                var libraryParameters = string.Join(",", libraryIdList.Select((_, i) => $"@lib{i}"));

                var baseQuery = BuildMappingSelectQuery();
                var sql = $@"{baseQuery} 
                            WHERE (sr.LibraryId IN ({libraryParameters}) OR c.LibraryId IN ({libraryParameters}))";

                using var connection = await _connectionFactory.CreateOpenConnectionAsync();
                
                using var command = new SqlCommand(sql, connection);

                for (int i = 0; i < libraryIdList.Count; i++)
                {
                    command.Parameters.AddWithValue($"@lib{i}", libraryIdList[i]);
                }

                var result = await ExecuteMappingReaderAsync(command);
                return result;
            }
            catch (SqlException sqlEx)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private string BuildMappingSelectQuery()
        {
            var query = @"SELECT csrm.isHidden, csrm.IsOverridden, sr.Guid as SecurityRequirementGuid, c.Guid as ComponentGuid
                        FROM ComponentSecurityRequirementMapping csrm
                        INNER JOIN SecurityRequirements sr ON csrm.SecurityRequirementId = sr.Id
                        INNER JOIN Components c ON csrm.ComponentId = c.Id";
            
            return query;
        }

        private async Task<IEnumerable<ComponentSecurityRequirementMapping>> ExecuteMappingReaderAsync(SqlCommand command)
        {
            try
            {
                var mappings = new List<ComponentSecurityRequirementMapping>();
                using var reader = await command.ExecuteReaderAsync();

                int recordCount = 0;
                while (await reader.ReadAsync())
                {
                    recordCount++;
                    var mapping = new ComponentSecurityRequirementMapping
                    {
                        SecurityRequirementGuid = (Guid)reader["SecurityRequirementGuid"],
                        ComponentGuid = (Guid)reader["ComponentGuid"],
                        IsHidden = (bool)reader["isHidden"],
                        IsOverridden = (bool)reader["IsOverridden"]
                    };
                    mappings.Add(mapping);
                    
                    
                }

                return mappings;
            }
            catch (SqlException sqlEx)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
