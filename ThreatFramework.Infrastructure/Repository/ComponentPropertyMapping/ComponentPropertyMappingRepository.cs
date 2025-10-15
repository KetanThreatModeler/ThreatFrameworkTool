using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using ThreatFramework.Core.PropertyMapping;
using ThreatFramework.Infra.Contract;
using ThreatFramework.Infra.Contract.Repository;

namespace ThreatFramework.Infrastructure.Repository
{
    public class ComponentPropertyMappingRepository : IComponentPropertyMappingRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;
        private readonly ILibraryCacheService _libraryCacheService;

        public ComponentPropertyMappingRepository(
            ISqlConnectionFactory connectionFactory,
            ILibraryCacheService libraryCacheService)
        {
            _connectionFactory = connectionFactory;
            _libraryCacheService = libraryCacheService;

        }


        public async Task<IEnumerable<ComponentPropertyMapping>> GetMappingsByLibraryIdAsync(IEnumerable<Guid> libraryGuids)
        {

            try
            {
                var libraryIds = await _libraryCacheService.GetIdsFromGuid(libraryGuids);

                if (!libraryIds.Any())
                {
                    return Enumerable.Empty<ComponentPropertyMapping>();
                }

                var libraryIdList = libraryIds.ToList();
                var libraryParameters = string.Join(",", libraryIdList.Select((_, i) => $"@lib{i}"));

                var baseQuery = BuildMappingSelectQuery();
                var sql = $@"{baseQuery} 
                            WHERE (p.LibraryId IN ({libraryParameters}) OR c.LibraryId IN ({libraryParameters}))";

               

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

        public async Task<IEnumerable<ComponentPropertyMapping>> GetReadOnlyMappingsAsync()
        {
            try
            {
                var readonlyLibraryIds = await _libraryCacheService.GetReadOnlyLibraryIdAsync();

                if (!readonlyLibraryIds.Any())
                {
                    return Enumerable.Empty<ComponentPropertyMapping>();
                }

                var libraryIdList = readonlyLibraryIds.ToList();
                var libraryParameters = string.Join(",", libraryIdList.Select((_, i) => $"@lib{i}"));

                var baseQuery = BuildMappingSelectQuery();
                var sql = $@"{baseQuery} 
                            WHERE (p.LibraryId IN ({libraryParameters}) OR c.LibraryId IN ({libraryParameters}))";


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
            var query = @"SELECT cpm.Id, cpm.IsOptional, cpm.isHidden, cpm.IsOverridden, p.Guid as PropertyGuid, c.Guid as ComponentGuid
                    FROM ComponentPropertyMapping cpm
                    INNER JOIN Properties p ON cpm.PropertyId = p.Id
                    INNER JOIN Components c ON cpm.ComponentId = c.Id";

            return query;
        }

        private async Task<IEnumerable<ComponentPropertyMapping>> ExecuteMappingReaderAsync(SqlCommand command)
        {
            try
            {
                var mappings = new List<ComponentPropertyMapping>();
                using var reader = await command.ExecuteReaderAsync();

                int recordCount = 0;
                while (await reader.ReadAsync())
                {
                    recordCount++;
                    var mapping = new ComponentPropertyMapping
                    {
                        Id = (int)reader["Id"],
                        PropertyGuid = (Guid)reader["PropertyGuid"],
                        ComponentGuid = (Guid)reader["ComponentGuid"],
                        IsOptional = (bool)reader["IsOptional"],
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
