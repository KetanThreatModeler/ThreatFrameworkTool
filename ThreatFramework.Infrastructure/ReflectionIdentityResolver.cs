using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Infra.Contract;

namespace ThreatFramework.Infrastructure
{
    public class ReflectionIdentityResolver : IIdentityResolver
    {
        public string GetEntityKey<T>(T entity)
        {
            if (entity is null) return string.Empty;

            // Prefer Guid property named "Guid"
            var guidProp = typeof(T).GetProperty("Guid", BindingFlags.Public | BindingFlags.Instance);
            if (guidProp?.PropertyType == typeof(Guid))
            {
                var g = (Guid)guidProp.GetValue(entity)!;
                if (g != default) return g.ToString();
            }

            // Fallback to Id (int) as string
            var idProp = typeof(T).GetProperty("Id", BindingFlags.Public | BindingFlags.Instance);
            if (idProp != null)
            {
                var v = idProp.GetValue(entity);
                return v?.ToString() ?? string.Empty;
            }

            // Last resort: hash of all public props
            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                 .Where(p => p.CanRead)
                                 .Select(p => p.GetValue(entity)?.ToString() ?? string.Empty);
            return string.Join("|", props).GetHashCode().ToString();
        }

        public Guid? GetLibraryGuid<T>(T entity)
        {
            if (entity is null) return null;

            // Try "LibraryGuid" first
            var libGuidProp = typeof(T).GetProperty("LibraryGuid", BindingFlags.Public | BindingFlags.Instance);
            if (libGuidProp?.PropertyType == typeof(Guid))
            {
                var v = (Guid)libGuidProp.GetValue(entity)!;
                return v == default ? null : v;
            }

            // Try "LibraryId" (Guid)
            var libIdProp = typeof(T).GetProperty("LibraryId", BindingFlags.Public | BindingFlags.Instance);
            if (libIdProp?.PropertyType == typeof(Guid))
            {
                var v = (Guid)libIdProp.GetValue(entity)!;
                return v == default ? null : v;
            }

            return null; // Not a library-scoped entity
        }
    }
}
