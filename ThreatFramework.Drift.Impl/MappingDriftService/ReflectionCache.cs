using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ThreatFramework.Drift.Contract.Model
{
    public static class ReflectionCache
    {
        private static readonly ConcurrentDictionary<(Type type, string nameLower), Func<object, object?>> _getterCache = new();

        public static object? Get(object instance, string propertyName, bool caseInsensitive = true)
        {
            var type = instance.GetType();
            var prop = FindProperty(type, propertyName, caseInsensitive)
                       ?? throw new MissingMemberException(type.FullName, propertyName);

            var getter = _getterCache.GetOrAdd((type, prop.Name.ToLowerInvariant()),
                _ => CompileGetter(type, prop));
            return getter(instance);
        }

        public static Guid? GetGuid<T>(T instance, string propertyName, bool caseInsensitive = true) where T : class
            => TryGetAsGuid(instance, propertyName, out var value, caseInsensitive) ? value : null;

        public static string? GetString<T>(T instance, string propertyName, bool caseInsensitive = true) where T : class
            => TryGetAsString(instance, propertyName, out var value, caseInsensitive) ? value : null;

        public static bool TryGetAsGuid<T>(T instance, string propertyName, out Guid? value, bool caseInsensitive = true) where T : class
        {
            var raw = TryGet(instance, propertyName, caseInsensitive, out var ok);
            if (!ok) { value = null; return false; }

            if (raw is Guid g) { value = g; return true; }
            if (raw is string s && Guid.TryParse(s, out var gs)) { value = gs; return true; }
            value = null; return false;
        }

        public static bool TryGetAsString<T>(T instance, string propertyName, out string? value, bool caseInsensitive = true) where T : class
        {
            var raw = TryGet(instance, propertyName, caseInsensitive, out var ok);
            value = ok ? raw?.ToString() : null;
            return ok;
        }

        private static object? TryGet(object instance, string propertyName, bool caseInsensitive, out bool ok)
        {
            var type = instance.GetType();
            var prop = FindProperty(type, propertyName, caseInsensitive);
            if (prop is null) { ok = false; return null; }

            var getter = _getterCache.GetOrAdd((type, prop.Name.ToLowerInvariant()),
                _ => CompileGetter(type, prop));
            ok = true;
            return getter(instance);
        }

        private static PropertyInfo? FindProperty(Type type, string name, bool caseInsensitive)
            => type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                   .FirstOrDefault(p => string.Equals(p.Name, name,
                                      caseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal));

        private static Func<object, object?> CompileGetter(Type type, PropertyInfo prop)
        {
            var instance = Expression.Parameter(typeof(object), "o");
            var casted = Expression.Convert(instance, type);
            var access = Expression.Property(casted, prop);
            var box = Expression.Convert(access, typeof(object));
            return Expression.Lambda<Func<object, object?>>(box, instance).Compile();
        }
    }
}
