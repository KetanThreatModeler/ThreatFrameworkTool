using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Infra.Contract;
using ThreatModeler.TF.Core.Model.CoreEntities;
using ThreatModeler.TF.Core.Model.Global;

namespace ThreatFramework.Infrastructure
{
    public class DefaultFolderMap : IFolderMap
    {
        private static readonly Dictionary<Type, string> _map = new()
        {
            { typeof(Threat), "threats" },
            { typeof(Component), "components" },
            { typeof(SecurityRequirement), "security-requirnment" },
            { typeof(TestCase), "test-cases" },
            { typeof(Property), "property" },
            { typeof(PropertyOption), "property-option" },
            { typeof(Library), "libraries" },
        };

        public string GetSubfolderFor<T>()
        {
            if (_map.TryGetValue(typeof(T), out var folder)) return folder;
            throw new InvalidOperationException($"No folder mapping for entity {typeof(T).Name}");
        }

        public bool TryGetEntityKindFromRelativePath(string relativePath, out string entityKind)
        {
            // relativePath like: "components/1.yaml"
            var first = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (first.Length == 0) { entityKind = string.Empty; return false; }

            string folder = first[0].Trim().ToLowerInvariant();
            foreach (var kvp in _map)
            {
                if (string.Equals(kvp.Value, folder, StringComparison.OrdinalIgnoreCase))
                {
                    entityKind = kvp.Key.Name; // e.g., "Component"
                    return true;
                }
            }
            entityKind = string.Empty;
            return false;
        }
    }
}
