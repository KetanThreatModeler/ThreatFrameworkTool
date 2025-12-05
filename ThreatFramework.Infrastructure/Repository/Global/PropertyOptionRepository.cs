using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using ThreatFramework.Infra.Contract;
using ThreatModeler.TF.Core.Global;
using ThreatModeler.TF.Infra.Contract.Repository.Global;

namespace ThreatModeler.TF.Infra.Implmentation.Repository.Global
{
    public class PropertyOptionRepository : IPropertyOptionRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;
        private readonly ILogger<PropertyOptionRepository> _logger;

        public PropertyOptionRepository(
            ISqlConnectionFactory sqlConnectionFactory,
            ILogger<PropertyOptionRepository> logger)
        {
            _connectionFactory = sqlConnectionFactory ?? throw new ArgumentNullException(nameof(sqlConnectionFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<Guid>> GetAllPropertyOptionGuidsAsync()
        {
            const string sql = "SELECT [Guid] FROM [dbo].[PropertyOptions];";

            _logger.LogInformation("Starting {Method}.", nameof(GetAllPropertyOptionGuidsAsync));

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
                    else
                    {
                        _logger.LogWarning("Encountered NULL Guid in PropertyOptions table. Row skipped.");
                    }
                }

                _logger.LogInformation(
                    "Completed {Method}. Retrieved {Count} PropertyOption GUIDs.",
                    nameof(GetAllPropertyOptionGuidsAsync),
                    guids.Count);

                return guids;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in {Method}.", nameof(GetAllPropertyOptionGuidsAsync));
                throw;
            }
        }

        public async Task<IEnumerable<PropertyOption>> GetAllPropertyOptionsAsync()
        {
            const string sql = @"
                SELECT 
                    [Id],
                    [PropertyId],
                    [IsDefault],
                    [isHidden],
                    [IsOverridden],
                    [Guid],
                    [OptionText],
                    [ChineseOptionText]
                FROM [dbo].[PropertyOptions];";

            _logger.LogInformation("Starting {Method}.", nameof(GetAllPropertyOptionsAsync));

            try
            {
                using var connection = await _connectionFactory.CreateOpenConnectionAsync().ConfigureAwait(false);
                using var command = new SqlCommand(sql, connection);

                var options = await ExecutePropertyOptionReaderAsync(command).ConfigureAwait(false);

                _logger.LogInformation(
                    "Completed {Method}. Retrieved {Count} PropertyOptions.",
                    nameof(GetAllPropertyOptionsAsync),
                    options.Count);

                return options;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in {Method}.", nameof(GetAllPropertyOptionsAsync));
                throw;
            }
        }

        private async Task<List<PropertyOption>> ExecutePropertyOptionReaderAsync(SqlCommand command)
        {
            var results = new List<PropertyOption>();

            using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);

            // Cache ordinals once
            var idOrdinal = reader.GetOrdinal("Id");
            var propertyIdOrdinal = reader.GetOrdinal("PropertyId");
            var isDefaultOrdinal = reader.GetOrdinal("IsDefault");
            var isHiddenOrdinal = reader.GetOrdinal("isHidden");       // actual column name
            var isOverriddenOrdinal = reader.GetOrdinal("IsOverridden");
            var guidOrdinal = reader.GetOrdinal("Guid");
            var optionTextOrdinal = reader.GetOrdinal("OptionText");
            var chineseOptionTextOrdinal = reader.GetOrdinal("ChineseOptionText");

            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                try
                {
                    // Handle possible NULL PropertyId → map to 0 and log once per row
                    int propertyGuidValue;
                    if (reader.IsDBNull(propertyIdOrdinal))
                    {
                        propertyGuidValue = 0;
                        _logger.LogWarning(
                            "PropertyOptions row with Id={Id} has NULL PropertyId. Mapping to 0 in PropertyGuid.",
                            reader.GetInt32(idOrdinal));
                    }
                    else
                    {
                        propertyGuidValue = reader.GetInt32(propertyIdOrdinal);
                    }

                    var option = new PropertyOption
                    {
                        // DB's PK Id is not exposed on the model, so we ignore it here.
                        PropertyGuid = propertyGuidValue,
                        Guid = reader.GetGuid(guidOrdinal),

                        IsDefault = !reader.IsDBNull(isDefaultOrdinal) && reader.GetBoolean(isDefaultOrdinal),
                        IsHidden = !reader.IsDBNull(isHiddenOrdinal) && reader.GetBoolean(isHiddenOrdinal),
                        IsOverridden = !reader.IsDBNull(isOverriddenOrdinal) && reader.GetBoolean(isOverriddenOrdinal),

                        OptionText = reader.IsDBNull(optionTextOrdinal)
                            ? string.Empty
                            : reader.GetString(optionTextOrdinal),

                        ChineseOptionText = reader.IsDBNull(chineseOptionTextOrdinal)
                            ? string.Empty
                            : reader.GetString(chineseOptionTextOrdinal)
                    };

                    results.Add(option);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to hydrate PropertyOption from current data row. Skipping row.");
                }
            }

            return results;
        }
    }
}
