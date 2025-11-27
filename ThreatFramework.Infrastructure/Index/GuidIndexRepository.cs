using ThreatFramework.Infra.Contract.Index;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ThreatFramework.Infrastructure.Index
{
    public sealed class GuidIndexRepository : IGuidIndexRepository
    {
        private static readonly IDeserializer _deserializer =
            new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();

        public async Task<IEnumerable<GuidIndex>> LoadAsync(string path, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Index file path must be provided.", nameof(path));
            }

            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Index file not found at '{path}'.", path);
            }

            string yaml = await File.ReadAllTextAsync(path, ct).ConfigureAwait(false);
            Dictionary<string, List<Dictionary<string, object>>> raw = _deserializer.Deserialize<Dictionary<string, List<Dictionary<string, object>>>>(yaml) ?? [];

            List<GuidIndex> result = [];
            if (raw.TryGetValue("index", out List<Dictionary<string, object>>? indexEntries))
            {
                foreach (Dictionary<string, object> entry in indexEntries)
                {
                    if (entry.TryGetValue("intId", out object? intIdObj) &&
                        entry.TryGetValue("Guid", out object? guidObj) &&
                        int.TryParse(intIdObj.ToString(), out int intId) &&
                        Guid.TryParse(guidObj.ToString(), out Guid guid))
                    {
                        Guid libraryId = Guid.Empty;
                        if (entry.TryGetValue("LibraryId", out object? libraryIdObj) &&
                            libraryIdObj != null &&
                            Guid.TryParse(libraryIdObj.ToString(), out Guid parsedLibraryId))
                        {
                            libraryId = parsedLibraryId;
                        }

                        EntityType entityType = EntityType.Library;
                        entityType = entry.TryGetValue("EntityType", out object? entityTypeObj) &&
                            entityTypeObj != null &&
                            Enum.TryParse<EntityType>(entityTypeObj.ToString(), true, out EntityType parsedEntityType)
                            ? parsedEntityType
                            : throw new InvalidDataException($"Invalid EntityType value in index file '{path}'.");

                        result.Add(new GuidIndex
                        {
                            Guid = guid,
                            Id = intId,
                            LibraryGuid = libraryId,
                            EntityType = entityType
                        });
                    }
                    else
                    {
                        throw new InvalidDataException($"Invalid entry format in index file '{path}'.");
                    }
                }
            }
            return result;
        }
    }
}
