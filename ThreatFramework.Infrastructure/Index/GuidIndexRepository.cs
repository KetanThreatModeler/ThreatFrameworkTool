using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Infra.Contract.Index;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ThreatFramework.Infrastructure.Index
{
    public sealed class GuidIndexRepository : IGuidIndexRepository
    {
        private static readonly IDeserializer _deserializer =
            new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();

        public async Task<IReadOnlyDictionary<Guid, int>> LoadAsync(string path, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Index file path must be provided.", nameof(path));

            if (!File.Exists(path))
                throw new FileNotFoundException($"Index file not found at '{path}'.", path);

            var yaml = await File.ReadAllTextAsync(path, ct).ConfigureAwait(false);
            var raw = _deserializer.Deserialize<Dictionary<string, int>>(yaml) ?? new();

            var result = new Dictionary<Guid, int>(raw.Count);
            foreach (var (k, v) in raw)
            {
                if (!Guid.TryParse(k, out var g))
                    throw new InvalidDataException($"Invalid GUID key '{k}' in '{path}'.");
                result[g] = v;
            }
            return result;
        }
    }
}
