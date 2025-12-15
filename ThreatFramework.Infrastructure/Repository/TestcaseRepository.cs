using Microsoft.Data.SqlClient;
using ThreatFramework.Infra.Contract;
using ThreatModeler.TF.Core.Model.CoreEntities;
using ThreatModeler.TF.Infra.Contract.Repository.CoreEntities;

namespace ThreatFramework.Infrastructure.Repository
{
    public class TestcaseRepository : ITestcaseRepository
    {
        private readonly ILibraryCacheService _libraryCacheService;
        private readonly ISqlConnectionFactory _connectionFactory;

        public TestcaseRepository(ILibraryCacheService libraryCacheService, ISqlConnectionFactory sqlConnectionFactory)
        {
            _libraryCacheService = libraryCacheService;
            _connectionFactory = sqlConnectionFactory;
        }

        public async Task<IEnumerable<TestCase>> GetReadOnlyTestcasesAsync()
        {
            var readonlyLibraryIds = await _libraryCacheService.GetReadOnlyLibraryIdAsync();

            if (!readonlyLibraryIds.Any())
                return Enumerable.Empty<TestCase>();

            var libraryIdList = readonlyLibraryIds.ToList();
            var libraryParameters = string.Join(",", libraryIdList.Select((_, i) => $"@lib{i}"));

            var sql = $@"{BuildTestCaseSelectQuery()} 
                        WHERE LibraryId IN ({libraryParameters})";

            using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            using var command = new SqlCommand(sql, connection);

            for (int i = 0; i < libraryIdList.Count; i++)
            {
                command.Parameters.AddWithValue($"@lib{i}", libraryIdList[i]);
            }

            return await ExecuteTestCaseReaderAsync(command);
        }

        public async Task<IEnumerable<TestCase>> GetTestcasesByLibraryIdAsync(IEnumerable<Guid> libraryIds)
        {
            var ids = await _libraryCacheService.GetIdsFromGuid(libraryIds);

            if (!ids.Any())
                return Enumerable.Empty<TestCase>();

            var libraryIdList = ids.ToList();
            var libraryParameters = string.Join(",", libraryIdList.Select((_, i) => $"@lib{i}"));

            var sql = $@"{BuildTestCaseSelectQuery()} 
                WHERE LibraryId IN ({libraryParameters})";

            using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            using var command = new SqlCommand(sql, connection);

            for (int i = 0; i < libraryIdList.Count; i++)
            {
                command.Parameters.AddWithValue($"@lib{i}", libraryIdList[i]);
            }

            return await ExecuteTestCaseReaderAsync(command);
        }

        private static string BuildTestCaseSelectQuery()
        {
            return @"SELECT tc.Id, tc.LibraryId, tc.isHidden, tc.IsOverridden, 
                            tc.CreatedDate, tc.LastUpdated, tc.Guid, tc.Name, tc.ChineseName, tc.Labels, 
                            tc.Description, tc.ChineseDescription 
                    FROM TestCases tc";
        }

        private async Task<IEnumerable<TestCase>> ExecuteTestCaseReaderAsync(SqlCommand command)
        {
            var testCases = new List<TestCase>();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                testCases.Add(new TestCase
                {
                    LibraryId = await _libraryCacheService.GetGuidByIdAsync((int)reader["LibraryId"]),
                    IsHidden = (bool)reader["isHidden"],
                    IsOverridden = (bool)reader["IsOverridden"],
                    Guid = (Guid)reader["Guid"],
                    Name = (string)reader["Name"],
                    ChineseName = reader["ChineseName"] as string,
                    Labels = reader["Labels"] as string,
                    Description = reader["Description"] as string,
                    ChineseDescription = reader["ChineseDescription"] as string
                });
            }

            return testCases;
        }

        public async Task<IEnumerable<Guid>> GetGuidsAsync()
        {
            var sql = "SELECT Guid FROM TestCases";
            
            using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            using var command = new SqlCommand(sql, connection);
            
            var guids = new List<Guid>();
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                guids.Add((Guid)reader["Guid"]);
            }
            
            return guids;
        }

        public async Task<IEnumerable<(Guid TestCaseGuid, Guid LibraryGuid)>> GetGuidsAndLibraryGuidsAsync()
        {
            const string sql = @"
        SELECT tc.Guid AS TestCaseGuid, l.Guid AS LibraryGuid
        FROM TestCases tc
        INNER JOIN Libraries l ON tc.LibraryId = l.Id";

            using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            using var command = new SqlCommand(sql, connection);

            var results = new List<(Guid, Guid)>();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var testCaseGuid = reader.GetGuid(reader.GetOrdinal("TestCaseGuid"));
                var libraryGuid = reader.GetGuid(reader.GetOrdinal("LibraryGuid"));

                results.Add((testCaseGuid, libraryGuid));
            }

            return results;
        }

        public async Task<IEnumerable<Guid>> GetGuidsByLibraryIds(IEnumerable<Guid> libraryIds)
        {
            if (libraryIds == null || !libraryIds.Any())
                return Enumerable.Empty<Guid>();

            // Convert library GUIDs to integer IDs used in the DB
            var ids = await _libraryCacheService.GetIdsFromGuid(libraryIds);

            if (!ids.Any())
                return Enumerable.Empty<Guid>();

            var libraryIdList = ids.ToList();
            var libraryParameters = string.Join(",", libraryIdList.Select((_, i) => $"@lib{i}"));

            var sql = $@"SELECT Guid
                 FROM TestCases
                 WHERE LibraryId IN ({libraryParameters})";

            using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            using var command = new SqlCommand(sql, connection);

            for (int i = 0; i < libraryIdList.Count; i++)
            {
                command.Parameters.AddWithValue($"@lib{i}", libraryIdList[i]);
            }

            var guids = new List<Guid>();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                guids.Add(reader.GetGuid(reader.GetOrdinal("Guid")));
            }

            return guids;
        }

        public async Task<IEnumerable<(Guid TestCaseGuid, Guid LibraryGuid)>> GetGuidsAndLibraryGuidsAsync(IEnumerable<Guid> libraryIds)
        {
            if (libraryIds == null || !libraryIds.Any())
                return Enumerable.Empty<(Guid TestCaseGuid, Guid LibraryGuid)>();

            // Convert library GUIDs to integer IDs used in the DB
            var ids = await _libraryCacheService.GetIdsFromGuid(libraryIds);

            if (!ids.Any())
                return Enumerable.Empty<(Guid TestCaseGuid, Guid LibraryGuid)>();

            var libraryIdList = ids.ToList();
            var libraryParameters = string.Join(",", libraryIdList.Select((_, i) => $"@lib{i}"));

            var sql = $@"
        SELECT tc.Guid AS TestCaseGuid, l.Guid AS LibraryGuid
        FROM TestCases tc
        INNER JOIN Libraries l ON tc.LibraryId = l.Id
        WHERE tc.LibraryId IN ({libraryParameters})";

            using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            using var command = new SqlCommand(sql, connection);

            for (int i = 0; i < libraryIdList.Count; i++)
            {
                command.Parameters.AddWithValue($"@lib{i}", libraryIdList[i]);
            }

            var results = new List<(Guid TestCaseGuid, Guid LibraryGuid)>();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var testCaseGuid = reader.GetGuid(reader.GetOrdinal("TestCaseGuid"));
                var libraryGuid = reader.GetGuid(reader.GetOrdinal("LibraryGuid"));

                results.Add((testCaseGuid, libraryGuid));
            }

            return results;
        }
    }
}
