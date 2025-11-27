using Microsoft.Data.SqlClient;
using ThreatFramework.Infra.Contract;
using ThreatModeler.TF.Core.Global;
using ThreatModeler.TF.Infra.Contract.Repository.Global;

namespace ThreatModeler.TF.Infra.Implmentation.Repository.Global
{
    public class ComponentTypeRepository : IComponentTypeRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;
        private readonly ILibraryCacheService _libraryCacheService;

        public ComponentTypeRepository(ILibraryCacheService libraryCacheService, ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _libraryCacheService = libraryCacheService ?? throw new ArgumentNullException(nameof(libraryCacheService));
        }


        public async Task<IEnumerable<ComponentType>> GetComponentTypesAsync()
        {
            const string sql = @"
            SELECT [Id],
                   [Name],
                   [Description],
                   [LibraryId],
                   [Guid],
                   [isHidden],
                   [IsSecurityControl],
                   [ChineseName],
                   [ChineseDescription]
            FROM [dbo].[ComponentTypes];";

            List<ComponentType> componentTypes = new();

            using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync();
            using SqlCommand command = new(sql, connection);
            using SqlDataReader reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                int libraryId = (int)reader["LibraryId"];
                Guid libraryGuid = await _libraryCacheService.GetGuidByIdAsync(libraryId);

                componentTypes.Add(new ComponentType
                {
                    Guid = (Guid)reader["Guid"],
                    Name = reader["Name"] as string ?? string.Empty,
                    Description = reader["Description"] as string ?? string.Empty,
                    LibraryGuid = libraryGuid,
                    IsHidden = (bool)reader["isHidden"],
                    IsSecurityControl = (bool)reader["IsSecurityControl"],
                    ChineseName = reader["ChineseName"] as string ?? string.Empty,
                    ChineseDescription = reader["ChineseDescription"] as string ?? string.Empty
                });
            }

            return componentTypes;
        }

        public async Task<IEnumerable<(Guid PropertyGuid, Guid LibraryGuid)>> GetGuidsAndLibraryGuidsAsync()
        {

            const string sql = @"
            SELECT 
              ct.Guid AS ComponentTypeGuid,
              l.Guid AS LibraryGuid
            FROM ComponentTypes ct
            INNER JOIN Libraries l ON ct.LibraryId = l.Id;";

            List<(Guid, Guid)> componentTypes = new();

            using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync();
            using SqlCommand command = new(sql, connection);
            using SqlDataReader reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                Guid libraryId = reader.GetGuid(reader.GetOrdinal("LibraryGuid"));
                Guid componentTypeGuid = reader.GetGuid(reader.GetOrdinal("ComponentTypeGuid"));
                componentTypes.Add((componentTypeGuid, libraryId));
            }
            return componentTypes;
        }
    }
}
