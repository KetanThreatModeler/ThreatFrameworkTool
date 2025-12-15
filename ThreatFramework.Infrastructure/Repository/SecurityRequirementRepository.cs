using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThreatFramework.Core.CoreEntities;
using ThreatFramework.Infra.Contract;
using ThreatFramework.Infra.Contract.Repository;
using ThreatModeler.TF.Infra.Implmentation.Helper;

namespace ThreatFramework.Infrastructure.Repository
{
    public class SecurityRequirementRepository : ISecurityRequirementRepository
    {
        private readonly ILibraryCacheService _libraryCacheService;
        private readonly ISqlConnectionFactory _connectionFactory;
        private readonly ILogger<SecurityRequirementRepository> _logger;

        public SecurityRequirementRepository(
            ILibraryCacheService libraryCacheService,
            ISqlConnectionFactory sqlConnectionFactory,
            ILogger<SecurityRequirementRepository> logger)
        {
            _libraryCacheService = libraryCacheService ?? throw new ArgumentNullException(nameof(libraryCacheService));
            _connectionFactory = sqlConnectionFactory ?? throw new ArgumentNullException(nameof(sqlConnectionFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region Public API

        public async Task<IEnumerable<SecurityRequirement>> GetReadOnlySecurityRequirementsAsync()
        {
            var readonlyLibraryIds = await _libraryCacheService
                .GetReadOnlyLibraryIdAsync()
                .ConfigureAwait(false);

            if (!readonlyLibraryIds.Any())
            {
                _logger.LogInformation("No read-only library IDs found. Returning empty SecurityRequirement list.");
                return Enumerable.Empty<SecurityRequirement>();
            }

            return await GetByLibraryIntIdsAsync(readonlyLibraryIds).ConfigureAwait(false);
        }

        public async Task<IEnumerable<SecurityRequirement>> GetSecurityRequirementsByLibraryIdAsync(
            IEnumerable<Guid> libraryIds)
        {
            if (libraryIds is null || !libraryIds.Any())
            {
                _logger.LogWarning("GetSecurityRequirementsByLibraryIdAsync called with null or empty libraryIds.");
                return Enumerable.Empty<SecurityRequirement>();
            }

            var ids = await _libraryCacheService
                .GetIdsFromGuid(libraryIds)
                .ConfigureAwait(false);

            if (!ids.Any())
            {
                _logger.LogInformation("No SQL library IDs resolved from provided GUIDs. Returning empty result.");
                return Enumerable.Empty<SecurityRequirement>();
            }

            return await GetByLibraryIntIdsAsync(ids).ConfigureAwait(false);
        }

        public async Task<IEnumerable<Guid>> GetGuidAsync()
        {
            const string sql = "SELECT Guid FROM SecurityRequirements";

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
                _logger.LogError(ex, "Failed to get SecurityRequirement GUIDs.");
                throw;
            }
        }

        public async Task<IEnumerable<(Guid SecurityRequirementGuid, Guid LibraryGuid)>> GetGuidsAndLibraryGuidsAsync()
        {
            const string sql = @"
                SELECT sr.Guid AS SecurityRequirementGuid, l.Guid AS LibraryGuid
                FROM SecurityRequirements sr
                INNER JOIN Libraries l ON sr.LibraryId = l.Id";

            return await FetchGuidPairsAsync(sql, null).ConfigureAwait(false);
        }

        public async Task<IEnumerable<Guid>> GetGuidsByLibraryIds(IEnumerable<Guid> libraryIds)
        {
            if (libraryIds is null || !libraryIds.Any())
            {
                _logger.LogWarning("GetGuidsByLibraryIds called with null or empty libraryIds.");
                return Enumerable.Empty<Guid>();
            }

            var ids = await _libraryCacheService
                .GetIdsFromGuid(libraryIds)
                .ConfigureAwait(false);

            if (!ids.Any())
            {
                _logger.LogInformation("No SQL library IDs resolved from provided GUIDs for GetGuidsByLibraryIds.");
                return Enumerable.Empty<Guid>();
            }

            var idList = ids.ToList();
            var parameterNames = string.Join(",", idList.Select((_, i) => $"@lib{i}"));

            var sql = $@"
                SELECT Guid
                FROM SecurityRequirements
                WHERE LibraryId IN ({parameterNames})";

            try
            {
                using var connection = await _connectionFactory.CreateOpenConnectionAsync().ConfigureAwait(false);
                using var command = new SqlCommand(sql, connection);

                for (var i = 0; i < idList.Count; i++)
                {
                    command.Parameters.AddWithValue($"@lib{i}", idList[i]);
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
                _logger.LogError(ex, "Failed to get SecurityRequirement GUIDs by library IDs.");
                throw;
            }
        }

        public async Task<IEnumerable<(Guid SecurityRequirementGuid, Guid LibraryGuid)>> GetGuidsAndLibraryGuidsAsync(
            IEnumerable<Guid> libraryIds)
        {
            if (libraryIds is null || !libraryIds.Any())
            {
                _logger.LogWarning("GetGuidsAndLibraryGuidsAsync called with null or empty libraryIds.");
                return Enumerable.Empty<(Guid SecurityRequirementGuid, Guid LibraryGuid)>();
            }

            var ids = await _libraryCacheService
                .GetIdsFromGuid(libraryIds)
                .ConfigureAwait(false);

            if (!ids.Any())
            {
                _logger.LogInformation("No SQL library IDs resolved from provided GUIDs for GetGuidsAndLibraryGuidsAsync.");
                return Enumerable.Empty<(Guid SecurityRequirementGuid, Guid LibraryGuid)>();
            }

            var idList = ids.ToList();
            var parameterNames = string.Join(",", idList.Select((_, i) => $"@lib{i}"));

            var sql = $@"
                SELECT sr.Guid AS SecurityRequirementGuid, l.Guid AS LibraryGuid
                FROM SecurityRequirements sr
                INNER JOIN Libraries l ON sr.LibraryId = l.Id
                WHERE sr.LibraryId IN ({parameterNames})";

            return await FetchGuidPairsAsync(sql, cmd =>
            {
                for (var i = 0; i < idList.Count; i++)
                {
                    cmd.Parameters.AddWithValue($"@lib{i}", idList[i]);
                }
            }).ConfigureAwait(false);
        }

        #endregion

        #region Private Query Builders

        /// <summary>
        /// Base SELECT including a join to Risks to get the Risk Name.
        /// Note: we do NOT expose RiskId, only RiskName.
        /// </summary>
        private static string BuildSecurityRequirementSelectQuery()
        {
            return @"
                SELECT 
                    sr.LibraryId,
                    sr.IsCompensatingControl,
                    sr.IsHidden,
                    sr.IsOverridden,
                    sr.Guid,
                    sr.Name,
                    sr.ChineseName,
                    sr.Labels,
                    sr.Description,
                    sr.ChineseDescription,
                    r.Name AS RiskName
                FROM SecurityRequirements sr
                LEFT JOIN Risks r ON sr.RiskId = r.Id";
        }

        #endregion

        #region Private Helpers

        /// <summary>
        /// Shared path to get security requirements by SQL integer library IDs.
        /// </summary>
        private async Task<IEnumerable<SecurityRequirement>> GetByLibraryIntIdsAsync(IEnumerable<int> libraryIds)
        {
            var libraryIdList = libraryIds.ToList();
            var parameterNames = string.Join(",", libraryIdList.Select((_, i) => $"@lib{i}"));

            var sql = $@"{BuildSecurityRequirementSelectQuery()}
                         WHERE sr.LibraryId IN ({parameterNames})";

            try
            {
                using var connection = await _connectionFactory.CreateOpenConnectionAsync().ConfigureAwait(false);
                using var command = new SqlCommand(sql, connection);

                for (var i = 0; i < libraryIdList.Count; i++)
                {
                    command.Parameters.AddWithValue($"@lib{i}", libraryIdList[i]);
                }

                return await ExecuteSecurityRequirementReaderAsync(command).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve SecurityRequirements by library IDs.");
                throw;
            }
        }

        /// <summary>
        /// Hydrates SecurityRequirement entities from a prepared SqlCommand.
        /// Only RiskName (from Risks.Name) is exposed; RiskId is not used.
        /// </summary>
        private async Task<IEnumerable<SecurityRequirement>> ExecuteSecurityRequirementReaderAsync(SqlCommand command)
        {
            var securityRequirements = new List<SecurityRequirement>();

            using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);

            // Cache ordinals
            var riskNameOrdinal = reader.GetOrdinal("RiskName");
            var libraryIdOrdinal = reader.GetOrdinal("LibraryId");
            var isCompensatingOrdinal = reader.GetOrdinal("IsCompensatingControl");
            var isHiddenOrdinal = reader.GetOrdinal("IsHidden");
            var isOverriddenOrdinal = reader.GetOrdinal("IsOverridden");
            var guidOrdinal = reader.GetOrdinal("Guid");
            var nameOrdinal = reader.GetOrdinal("Name");
            var chineseNameOrdinal = reader.GetOrdinal("ChineseName");
            var labelsOrdinal = reader.GetOrdinal("Labels");
            var descriptionOrdinal = reader.GetOrdinal("Description");
            var chineseDescriptionOrdinal = reader.GetOrdinal("ChineseDescription");

            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                try
                {
                    var sqlLibraryId = reader.GetInt32(libraryIdOrdinal);
                    var libraryGuid = await _libraryCacheService
                        .GetGuidByIdAsync(sqlLibraryId)
                        .ConfigureAwait(false);

                    var riskName = reader.IsDBNull(riskNameOrdinal)
                        ? string.Empty
                        : reader.GetString(riskNameOrdinal);

                    var sr = new SecurityRequirement
                    {
                        RiskName = riskName,
                        LibraryId = libraryGuid,
                        IsCompensatingControl = !reader.IsDBNull(isCompensatingOrdinal) && reader.GetBoolean(isCompensatingOrdinal),
                        IsHidden = !reader.IsDBNull(isHiddenOrdinal) && reader.GetBoolean(isHiddenOrdinal),
                        IsOverridden = !reader.IsDBNull(isOverriddenOrdinal) && reader.GetBoolean(isOverriddenOrdinal),
                        Guid = reader.GetGuid(guidOrdinal),
                        Name = reader.IsDBNull(nameOrdinal) ? string.Empty : reader.GetString(nameOrdinal),
                        ChineseName = reader.IsDBNull(chineseNameOrdinal) ? null : reader.GetString(chineseNameOrdinal),
                        Labels = reader.GetValue(labelsOrdinal).ToLabelList(),
                        Description = reader.IsDBNull(descriptionOrdinal) ? null : reader.GetString(descriptionOrdinal),
                        ChineseDescription = reader.IsDBNull(chineseDescriptionOrdinal) ? null : reader.GetString(chineseDescriptionOrdinal)
                    };

                    securityRequirements.Add(sr);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to hydrate SecurityRequirement row. Skipping current row.");
                }
            }

            return securityRequirements;
        }

        /// <summary>
        /// Shared helper for GUID/library GUID pairs.
        /// </summary>
        private async Task<IEnumerable<(Guid SecurityRequirementGuid, Guid LibraryGuid)>> FetchGuidPairsAsync(
            string sql,
            Action<SqlCommand>? configureCommand)
        {
            try
            {
                using var connection = await _connectionFactory.CreateOpenConnectionAsync().ConfigureAwait(false);
                using var command = new SqlCommand(sql, connection);

                configureCommand?.Invoke(command);

                var results = new List<(Guid SecurityRequirementGuid, Guid LibraryGuid)>();
                using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);

                var srGuidOrdinal = reader.GetOrdinal("SecurityRequirementGuid");
                var libraryGuidOrdinal = reader.GetOrdinal("LibraryGuid");

                while (await reader.ReadAsync().ConfigureAwait(false))
                {
                    var srGuid = reader.GetGuid(srGuidOrdinal);
                    var libraryGuid = reader.GetGuid(libraryGuidOrdinal);

                    results.Add((srGuid, libraryGuid));
                }

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get SecurityRequirement GUIDs and Library GUIDs.");
                throw;
            }
        }

        #endregion
    }
}
