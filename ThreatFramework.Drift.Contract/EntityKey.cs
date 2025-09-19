using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Drift.Contract.Model;

namespace ThreatFramework.Drift.Contract
{
    public static class EntityKey
    {
        /// <summary>
        /// Returns entity key as string by looking for "Id" or "Guid" (in that order).
        /// Throws if neither exists.
        /// </summary>
        public static string Of<T>(T entity) where T : class
        {
            // Prefer Id, then Guid
            if (ReflectionCache.TryGetAsString(entity, "Id", out var id) && !string.IsNullOrWhiteSpace(id))
                return id;
            if (ReflectionCache.TryGetAsString(entity, "Guid", out var gid) && !string.IsNullOrWhiteSpace(gid))
                return gid;

            throw new InvalidOperationException(
                $"Entity of type {typeof(T).Name} does not expose an 'Id' or 'Guid' property.");
        }
    }
}
