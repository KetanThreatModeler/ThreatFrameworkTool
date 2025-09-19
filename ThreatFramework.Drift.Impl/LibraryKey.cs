using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Drift.Contract.Model;

namespace ThreatFramework.Drift.Impl
{
    internal readonly record struct LibraryKey(Guid Guid, string Name)
    {
        public static LibraryKey From<T>(T entity) where T : class
        {
            var guid = ReflectionCache.GetGuid(entity, "LibraryGuid") ?? Guid.Empty;
            var name = ReflectionCache.GetString(entity, "LibraryName") ?? "(Unassigned)";
            return new LibraryKey(guid, string.IsNullOrWhiteSpace(name) ? "(Unassigned)" : name);
        }
    }
}
