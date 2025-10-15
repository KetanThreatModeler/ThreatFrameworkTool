using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Infra.Contract.DataInsertion;
using ThreatFramework.Infra.Contract.DataInsertion.Dto;

namespace ThreatFramework.Infrastructure.DataInsertion
{
    public sealed class SqlServerGuidLookupRepository : IGuidLookupRepository
    {
        private readonly ISqlConnectionFactory _connections;

        public SqlServerGuidLookupRepository(ISqlConnectionFactory connections)
        {
            _connections = connections ?? throw new ArgumentNullException(nameof(connections));
        }

        public async Task<MissingGuidsByEntity> GetMissingGuidsAsync(
            CheckMissingGuidsRequest request)
        {
            request ??= new CheckMissingGuidsRequest();

            // only care about Threats, SecurityRequirements, Properties
            var noThreats = (request.ThreatIds?.Count ?? 0) == 0;
            var noSecReqs = (request.SecurityRequirementIds?.Count ?? 0) == 0;
            var noProps = (request.PropertyIds?.Count ?? 0) == 0;

            if (noThreats && noSecReqs && noProps)
                return new MissingGuidsByEntity();

            // table variables survive across multiple SELECT statements
            const string sql = @"
DECLARE @Thr  XML = @ThreatXml,
        @Sec  XML = @SecurityReqXml,
        @Prop XML = @PropertyXml;

DECLARE @ThrIds  TABLE (Id UNIQUEIDENTIFIER PRIMARY KEY);
DECLARE @SecIds  TABLE (Id UNIQUEIDENTIFIER PRIMARY KEY);
DECLARE @PropIds TABLE (Id UNIQUEIDENTIFIER PRIMARY KEY);

INSERT INTO @ThrIds  SELECT X.N.value('.', 'uniqueidentifier') FROM @Thr.nodes('/r/g')  AS X(N);
INSERT INTO @SecIds  SELECT X.N.value('.', 'uniqueidentifier') FROM @Sec.nodes('/r/g')  AS X(N);
INSERT INTO @PropIds SELECT X.N.value('.', 'uniqueidentifier') FROM @Prop.nodes('/r/g') AS X(N);

-- 1) Threats missing
SELECT Tx.Id
FROM @ThrIds AS Tx
LEFT JOIN dbo.Threats AS T ON T.Guid = Tx.Id
WHERE T.Guid IS NULL;

-- 2) SecurityRequirements missing
SELECT Sx.Id
FROM @SecIds AS Sx
LEFT JOIN dbo.SecurityRequirements AS S ON S.Guid = Sx.Id
WHERE S.Guid IS NULL;

-- 3) Properties missing
SELECT Px.Id
FROM @PropIds AS Px
LEFT JOIN dbo.Properties AS P ON P.Guid = Px.Id
WHERE P.Guid IS NULL;";

            var result = new MissingGuidsByEntity(); // Libraries/Components remain empty

            await using var conn = await _connections.CreateOpenConnectionAsync().ConfigureAwait(false);
            await using var cmd = new SqlCommand(sql, conn)
            {
                CommandType = CommandType.Text,
                CommandTimeout = 0
            };

            cmd.Parameters.Add("@ThreatXml", SqlDbType.Xml).Value = ToXml(request.ThreatIds);
            cmd.Parameters.Add("@SecurityReqXml", SqlDbType.Xml).Value = ToXml(request.SecurityRequirementIds);
            cmd.Parameters.Add("@PropertyXml", SqlDbType.Xml).Value = ToXml(request.PropertyIds);

            // ONE round trip
            await using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);

            await ReadIntoAsync(reader, result.Threats);                  // result set 1
            await NextAndReadAsync(reader, result.SecurityRequirements);  // result set 2
            await NextAndReadAsync(reader, result.Properties);            // result set 3

            return result;
        }

        // --- DRY helpers ---
        private static async Task ReadIntoAsync(SqlDataReader reader, ISet<Guid> target)
        {
            while (await reader.ReadAsync().ConfigureAwait(false))
                target.Add(reader.GetGuid(0));
        }

        private static async Task NextAndReadAsync(SqlDataReader reader, ISet<Guid> target)
        {
            await reader.NextResultAsync().ConfigureAwait(false);
            await ReadIntoAsync(reader, target).ConfigureAwait(false);
        }

        private static string ToXml(IEnumerable<Guid>? ids)
        {
            var sb = new StringBuilder("<r>");
            if (ids != null)
            {
                foreach (var id in ids)
                    sb.Append("<g>").Append(id).Append("</g>");
            }
            sb.Append("</r>");
            return sb.ToString();
        }
    }
}