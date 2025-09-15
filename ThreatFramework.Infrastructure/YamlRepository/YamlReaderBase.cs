using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ThreatFramework.Infrastructure.YamlRepository
{
    public abstract class YamlReaderBase
    {
        protected readonly IDeserializer Deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        protected static IEnumerable<string> EnumerateYamlFiles(string folderPath)
        {
            if (!Directory.Exists(folderPath)) throw new DirectoryNotFoundException(folderPath);
            return Directory.EnumerateFiles(folderPath, "*.*", SearchOption.AllDirectories)
                .Where(p => p.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) || p.EndsWith(".yml", StringComparison.OrdinalIgnoreCase));
        }

        protected static string? ReadKind(string yaml)
        {
            try
            {
                var stream = new YamlStream();
                using var sr = new StringReader(yaml);
                stream.Load(sr);
                if (stream.Documents.Count == 0) return null;
                if (stream.Documents[0].RootNode is not YamlMappingNode root) return null;
                var key = new YamlScalarNode("kind");
                return root.Children.TryGetValue(key, out var v) && v is YamlScalarNode s ? s.Value : null;
            }
            catch { return null; }
        }

        protected static Guid G(string? raw, string field, string file)
        {
            if (Guid.TryParse(raw, out var g)) return g;
            throw new InvalidOperationException($"Invalid or missing GUID for '{field}' in '{file}' (value: '{raw ?? "<null>"}').");
        }

        protected static bool TryLoadRoot(string yaml, out YamlMappingNode root)
        {
            root = default!;
            try
            {
                var stream = new YamlStream();
                using var sr = new StringReader(yaml);
                stream.Load(sr);
                if (stream.Documents.Count == 0) return false;
                if (stream.Documents[0].RootNode is not YamlMappingNode r) return false;
                root = r;
                return true;
            }
            catch { return false; }
        }

        /// <summary>Parse YAML and return the "spec" mapping (and root) if present.</summary>
        protected static bool TryLoadSpec(string yaml, out YamlMappingNode spec, out YamlMappingNode root)
        {
            spec = default!;
            if (!TryLoadRoot(yaml, out root)) return false;
            return TryGetMap(root, "spec", out spec);
        }

        protected static bool TryGetMap(YamlMappingNode map, string key, out YamlMappingNode child)
        {
            var k = new YamlScalarNode(key);
            if (map.Children.TryGetValue(k, out var node) && node is YamlMappingNode m)
            {
                child = m;
                return true;
            }
            child = default!;
            return false;
        }

        protected static bool TryGetScalar(YamlMappingNode map, string key, out string value)
        {
            value = "";
            var k = new YamlScalarNode(key);
            if (map.Children.TryGetValue(k, out var node) && node is YamlScalarNode s && s.Value is not null)
            {
                value = s.Value;
                return true;
            }
            return false;
        }

        protected static string RequiredScalar(YamlMappingNode map, string key, string file)
        {
            if (TryGetScalar(map, key, out var v) && !string.IsNullOrWhiteSpace(v)) return v;
            throw new InvalidOperationException($"Missing required field '{key}' in '{file}'.");
        }

        protected static bool GetBool(YamlMappingNode map, string key, bool @default = false)
            => TryGetScalar(map, key, out var raw) && bool.TryParse(raw, out var b) ? b : @default;
    }
}

