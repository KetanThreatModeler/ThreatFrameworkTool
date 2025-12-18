using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using ThreatFramework.Infra.Contract;
using ThreatModeler.TF.Core.Model.CoreEntities;
using ThreatModeler.TF.Infra.Contract.Repository.CoreEntities;
using ThreatModeler.TF.Infra.Implmentation.Helper;

namespace ThreatModeler.TF.Infra.Implmentation.Repository.CoreEntities
{
    public class ThreatRepository : IThreatRepository
    {
        private readonly ILibraryCacheService _libraryCacheService;
        private readonly ISqlConnectionFactory _connectionFactory;
        private readonly ILogger<ThreatRepository> _logger;

        public ThreatRepository(
            ILibraryCacheService libraryCacheService,
            ISqlConnectionFactory sqlConnectionFactory,
            ILogger<ThreatRepository> logger)
        {
            _libraryCacheService = libraryCacheService ?? throw new ArgumentNullException(nameof(libraryCacheService));
            _connectionFactory = sqlConnectionFactory ?? throw new ArgumentNullException(nameof(sqlConnectionFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<Threat>> GetReadOnlyThreatsAsync()
        {
            _logger.LogInformation("Starting execution of {Method}.", nameof(GetReadOnlyThreatsAsync));

            try
            {
                var readonlyLibraryIds = await _libraryCacheService
                    .GetReadOnlyLibraryIdAsync()
                    .ConfigureAwait(false);

                if (!readonlyLibraryIds.Any())
                {
                    _logger.LogInformation("No read-only libraries found. Returning empty threat list.");
                    return Enumerable.Empty<Threat>();
                }

                return await GetByLibraryIntIdsAsync(readonlyLibraryIds).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in {Method}.", nameof(GetReadOnlyThreatsAsync));
                throw;
            }
        }

        public async Task<IEnumerable<Threat>> GetThreatsByLibraryIdAsync(IEnumerable<Guid> libraryGuids)
        {
            _logger.LogInformation(
                "Starting {Method} for {Count} library GUIDs.",
                nameof(GetThreatsByLibraryIdAsync),
                libraryGuids?.Count() ?? 0);

            try
            {
                if (libraryGuids is null || !libraryGuids.Any())
                {
                    _logger.LogWarning("{Method} called with null or empty libraryGuids.", nameof(GetThreatsByLibraryIdAsync));
                    return Enumerable.Empty<Threat>();
                }

                var ids = await _libraryCacheService
                    .GetIdsFromGuid(libraryGuids)
                    .ConfigureAwait(false);

                if (!ids.Any())
                {
                    _logger.LogWarning("No matching Library IDs found for the provided GUIDs.");
                    return Enumerable.Empty<Threat>();
                }

                return await GetByLibraryIntIdsAsync(ids).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in {Method}.", nameof(GetThreatsByLibraryIdAsync));
                throw;
            }
        }

        public async Task<IEnumerable<Guid>> GetGuidsAsync()
        {
            const string sql = "SELECT t.Guid FROM Threats t";

            try
            {
                using var connection = await _connectionFactory.CreateOpenConnectionAsync().ConfigureAwait(false);
                using var command = new SqlCommand(sql, connection);
                using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);

                var guids = new List<Guid>();
                var guidOrdinal = reader.GetOrdinal("Guid");

                while (await reader.ReadAsync().ConfigureAwait(false))
                {
                    if (!reader.IsDBNull(guidOrdinal))
                    {
                        guids.Add(reader.GetGuid(guidOrdinal));
                    }
                }

                return guids;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all Threat GUIDs.");
                throw;
            }
        }

        public async Task<IEnumerable<(Guid ThreatGuid, Guid LibraryGuid)>> GetGuidsAndLibraryGuidsAsync()
        {
            const string sql = @"
                SELECT t.Guid AS ThreatGuid, l.Guid AS LibraryGuid
                FROM Threats t
                INNER JOIN Libraries l ON t.LibraryId = l.Id";

            return await FetchThreatLibraryGuidPairsAsync(sql, null).ConfigureAwait(false);
        }

        public async Task<IEnumerable<Guid>> GetGuidsByLibraryIds(IEnumerable<Guid> libraryIds)
        {
            try
            {
                if (libraryIds is null || !libraryIds.Any())
                {
                    _logger.LogInformation("{Method} called with empty libraryIds.", nameof(GetGuidsByLibraryIds));
                    return Enumerable.Empty<Guid>();
                }

                var ids = await _libraryCacheService
                    .GetIdsFromGuid(libraryIds)
                    .ConfigureAwait(false);

                if (!ids.Any())
                {
                    _logger.LogInformation("No matching Library IDs found for provided GUIDs in {Method}.", nameof(GetGuidsByLibraryIds));
                    return Enumerable.Empty<Guid>();
                }

                var idList = ids.ToList();
                var libraryParameters = string.Join(",", idList.Select((_, i) => $"@lib{i}"));

                var sql = $@"SELECT Guid FROM Threats WHERE LibraryId IN ({libraryParameters})";

                using var connection = await _connectionFactory.CreateOpenConnectionAsync().ConfigureAwait(false);
                using var command = new SqlCommand(sql, connection);

                for (var i = 0; i < idList.Count; i++)
                {
                    _ = command.Parameters.AddWithValue($"@lib{i}", idList[i]);
                }

                var guids = new List<Guid>();
                using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
                var guidOrdinal = reader.GetOrdinal("Guid");

                while (await reader.ReadAsync().ConfigureAwait(false))
                {
                    if (!reader.IsDBNull(guidOrdinal))
                    {
                        guids.Add(reader.GetGuid(guidOrdinal));
                    }
                }

                return guids;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Threat GUIDs by Library Ids.");
                throw;
            }
        }

        public async Task<IEnumerable<(Guid ThreatGuid, Guid LibraryGuid)>> GetGuidsAndLibraryGuidsAsync(
            IEnumerable<Guid> libraryIds)
        {
            _logger.LogInformation(
                "Starting {Method} for {Count} libraries.",
                nameof(GetGuidsAndLibraryGuidsAsync),
                libraryIds?.Count() ?? 0);

            try
            {
                if (libraryIds == null || !libraryIds.Any())
                {
                    _logger.LogInformation("No library GUIDs provided. Returning empty result.");
                    return Enumerable.Empty<(Guid ThreatGuid, Guid LibraryGuid)>();
                }

                var ids = await _libraryCacheService
                    .GetIdsFromGuid(libraryIds)
                    .ConfigureAwait(false);

                if (!ids.Any())
                {
                    _logger.LogWarning("No matching Library IDs found for the provided GUIDs.");
                    return Enumerable.Empty<(Guid ThreatGuid, Guid LibraryGuid)>();
                }

                var idList = ids.ToList();
                var libraryParameters = string.Join(",", idList.Select((_, i) => $"@lib{i}"));

                var sql = $@"
                    SELECT t.Guid AS ThreatGuid, l.Guid AS LibraryGuid
                    FROM Threats t
                    INNER JOIN Libraries l ON t.LibraryId = l.Id
                    WHERE t.LibraryId IN ({libraryParameters})";

                return await FetchThreatLibraryGuidPairsAsync(sql, cmd =>
                {
                    for (var i = 0; i < idList.Count; i++)
                    {
                        _ = cmd.Parameters.AddWithValue($"@lib{i}", idList[i]);
                    }
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Threat/Library GUID pairs for specified libraries.");
                throw;
            }
        }

        #region Private Query Builders

        /// <summary>
        /// Base SELECT including a join to Risks to get the Risk Name.
        /// We no longer use RiskId at the domain level; only RiskName.
        /// </summary>
        private static string BuildThreatSelectQuery()
        {
            return @"
        SELECT 
            t.LibraryId,
            t.Automated,
            t.[isHidden] AS IsHidden,
            t.Guid,
            t.Name,
            t.ChineseName,
            t.Labels,
            t.Description,
            t.Reference,
            t.Intelligence,
            t.ChineseDescription,
            r.Name AS RiskName
        FROM Threats t
        LEFT JOIN Risks r ON t.RiskId = r.Id";
        }


        #endregion

        #region Private Helpers

        private async Task<IEnumerable<Threat>> GetByLibraryIntIdsAsync(IEnumerable<int> libraryIds)
        {
            var libraryIdList = libraryIds.ToList();
            var libraryParameters = string.Join(",", libraryIdList.Select((_, i) => $"@lib{i}"));

            var sql = $@"{BuildThreatSelectQuery()}
                         WHERE t.LibraryId IN ({libraryParameters})";

            try
            {
                using var connection = await _connectionFactory.CreateOpenConnectionAsync().ConfigureAwait(false);
                using var command = new SqlCommand(sql, connection);

                for (var i = 0; i < libraryIdList.Count; i++)
                {
                    _ = command.Parameters.AddWithValue($"@lib{i}", libraryIdList[i]);
                }

                return await ExecuteThreatReaderAsync(command).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve Threats by library IDs.");
                throw;
            }
        }

        private async Task<IEnumerable<Threat>> ExecuteThreatReaderAsync(SqlCommand command)
        {
            var threats = new List<Threat>();

            using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);

            var libraryIdOrdinal = reader.GetOrdinal("LibraryId");
            var automatedOrdinal = reader.GetOrdinal("Automated");
            var isHiddenOrdinal = reader.GetOrdinal("IsHidden");   // from alias
            var guidOrdinal = reader.GetOrdinal("Guid");
            var nameOrdinal = reader.GetOrdinal("Name");
            var chineseNameOrdinal = reader.GetOrdinal("ChineseName");
            var labelsOrdinal = reader.GetOrdinal("Labels");
            var descriptionOrdinal = reader.GetOrdinal("Description");
            var referenceOrdinal = reader.GetOrdinal("Reference");
            var intelligenceOrdinal = reader.GetOrdinal("Intelligence");
            var chineseDescriptionOrdinal = reader.GetOrdinal("ChineseDescription");
            var riskNameOrdinal = reader.GetOrdinal("RiskName");

            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                try
                {
                    var sqlLibraryId = reader.GetInt32(libraryIdOrdinal);
                    var libraryGuid = await _libraryCacheService
                        .GetGuidByIdAsync(sqlLibraryId)
                        .ConfigureAwait(false);

                    var threat = new Threat
                    {
                        RiskName = reader.IsDBNull(riskNameOrdinal) ? string.Empty : reader.GetString(riskNameOrdinal),

                        LibraryGuid = libraryGuid,
                        Automated = !reader.IsDBNull(automatedOrdinal) && reader.GetBoolean(automatedOrdinal),
                        IsHidden = !reader.IsDBNull(isHiddenOrdinal) && reader.GetBoolean(isHiddenOrdinal),

                        // Column doesn't exist in Threats table per your script; keep behavior stable
                        IsOverridden = false,

                        Guid = reader.GetGuid(guidOrdinal),

                        Name = reader.IsDBNull(nameOrdinal) ? string.Empty : reader["Name"].ToSafeString(),
                        ChineseName = reader.IsDBNull(chineseNameOrdinal) ? string.Empty : reader["ChineseName"].ToSafeString(),
                        Description = reader.IsDBNull(descriptionOrdinal) ? string.Empty : reader["Description"].ToSafeString(),
                        Reference = reader.IsDBNull(referenceOrdinal) ? string.Empty : reader["Reference"].ToSafeString(),
                        Intelligence = reader.IsDBNull(intelligenceOrdinal) ? string.Empty : reader["Intelligence"].ToSafeString(),
                        ChineseDescription = reader.IsDBNull(chineseDescriptionOrdinal) ? string.Empty : reader["ChineseDescription"].ToSafeString(),
                        Labels = reader.IsDBNull(labelsOrdinal) ? new List<string>() : reader["Labels"].ToLabelList()
                    };

                    threats.Add(threat);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to hydrate Threat from current data row. Skipping row.");
                }
            }

            return threats;
        }


        private async Task<IEnumerable<(Guid ThreatGuid, Guid LibraryGuid)>> FetchThreatLibraryGuidPairsAsync(
            string sql,
            Action<SqlCommand>? configureCommand)
        {
            try
            {
                using var connection = await _connectionFactory.CreateOpenConnectionAsync().ConfigureAwait(false);
                using var command = new SqlCommand(sql, connection);

                configureCommand?.Invoke(command);

                var results = new List<(Guid ThreatGuid, Guid LibraryGuid)>();
                using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);

                var threatGuidOrdinal = reader.GetOrdinal("ThreatGuid");
                var libraryGuidOrdinal = reader.GetOrdinal("LibraryGuid");

                while (await reader.ReadAsync().ConfigureAwait(false))
                {
                    var threatGuid = reader.GetGuid(threatGuidOrdinal);
                    var libraryGuid = reader.GetGuid(libraryGuidOrdinal);
                    results.Add((threatGuid, libraryGuid));
                }

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Threat/Library GUID pairs.");
                throw;
            }
        }
        #endregion
    }
}
