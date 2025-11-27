using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Infra.Contract.Index;

namespace ThreatModeler.TF.Infra.Implmentation.Index
{
    public sealed class GuidIndexData
    {
        public IReadOnlyDictionary<EntityType, IReadOnlyDictionary<Guid, int>> TypeGuidToIdMap { get; }
        public IReadOnlyDictionary<Guid, IReadOnlyDictionary<EntityType, IReadOnlySet<Guid>>> LibraryTypeGuidsMap { get; }
        public int MaxId { get; }
        public int Count { get; }

        public GuidIndexData(IEnumerable<GuidIndex> indices)
        {
            var typeMap = new Dictionary<EntityType, Dictionary<Guid, int>>();
            var libraryTypeGuidsMap = new Dictionary<Guid, Dictionary<EntityType, HashSet<Guid>>>();
            var maxId = 0;
            var totalCount = 0;

            foreach (var index in indices)
            {
                // Build type -> guid -> id mapping
                if (!typeMap.TryGetValue(index.EntityType, out var guidMap))
                {
                    guidMap = new Dictionary<Guid, int>();
                    typeMap[index.EntityType] = guidMap;
                }
                guidMap[index.Guid] = index.Id;

                // Build library -> type -> guids mapping
                if (!libraryTypeGuidsMap.TryGetValue(index.LibraryGuid, out var typeGuidsMap))
                {
                    typeGuidsMap = new Dictionary<EntityType, HashSet<Guid>>();
                    libraryTypeGuidsMap[index.LibraryGuid] = typeGuidsMap;
                }
                if (!typeGuidsMap.TryGetValue(index.EntityType, out var guids))
                {
                    guids = new HashSet<Guid>();
                    typeGuidsMap[index.EntityType] = guids;
                }
                guids.Add(index.Guid);

                totalCount++;
                if (index.Id > maxId)
                    maxId = index.Id;
            }

            TypeGuidToIdMap = typeMap.ToDictionary(
                kv => kv.Key,
                kv => (IReadOnlyDictionary<Guid, int>)kv.Value
            );

            LibraryTypeGuidsMap = libraryTypeGuidsMap.ToDictionary(
                kv => kv.Key,
                kv => (IReadOnlyDictionary<EntityType, IReadOnlySet<Guid>>)kv.Value.ToDictionary(
                    tkv => tkv.Key,
                    tkv => (IReadOnlySet<Guid>)tkv.Value
                )
            );

            MaxId = maxId;
            Count = totalCount;
        }

        public GuidIndexData(Dictionary<Guid, int> guidMap, IEnumerable<EntityIdentifier> entities)
        {
            var typeMap = new Dictionary<EntityType, Dictionary<Guid, int>>();
            var libraryTypeGuidsMap = new Dictionary<Guid, Dictionary<EntityType, HashSet<Guid>>>();
            var maxId = 0;
            var totalCount = 0;

            foreach (var entity in entities)
            {
                var id = guidMap[entity.Guid];

                // Build type -> guid -> id mapping
                if (!typeMap.TryGetValue(entity.EntityType, out var guidToIdMap))
                {
                    guidToIdMap = new Dictionary<Guid, int>();
                    typeMap[entity.EntityType] = guidToIdMap;
                }
                guidToIdMap[entity.Guid] = id;

                // Build library -> type -> guids mapping
                if (!libraryTypeGuidsMap.TryGetValue(entity.LibraryGuid, out var typeGuidsMap))
                {
                    typeGuidsMap = new Dictionary<EntityType, HashSet<Guid>>();
                    libraryTypeGuidsMap[entity.LibraryGuid] = typeGuidsMap;
                }
                if (!typeGuidsMap.TryGetValue(entity.EntityType, out var guids))
                {
                    guids = new HashSet<Guid>();
                    typeGuidsMap[entity.EntityType] = guids;
                }
                guids.Add(entity.Guid);

                totalCount++;
                if (id > maxId)
                    maxId = id;
            }

            TypeGuidToIdMap = typeMap.ToDictionary(
                kv => kv.Key,
                kv => (IReadOnlyDictionary<Guid, int>)kv.Value
            );

            LibraryTypeGuidsMap = libraryTypeGuidsMap.ToDictionary(
                kv => kv.Key,
                kv => (IReadOnlyDictionary<EntityType, IReadOnlySet<Guid>>)kv.Value.ToDictionary(
                    tkv => tkv.Key,
                    tkv => (IReadOnlySet<Guid>)tkv.Value
                )
            );

            MaxId = maxId;
            Count = totalCount;
        }

        public bool TryGetId(Guid guid, out int id)
        {
            foreach (var typeMap in TypeGuidToIdMap.Values)
            {
                if (typeMap.TryGetValue(guid, out id))
                    return true;
            }
            id = 0;
            return false;
        }

        public bool TryGetId(EntityType entityType, Guid guid, out int id)
        {
            if (TypeGuidToIdMap.TryGetValue(entityType, out var guidMap))
                return guidMap.TryGetValue(guid, out id);

            id = 0;
            return false;
        }

        public IReadOnlyDictionary<Guid, int>? GetGuidsForType(EntityType entityType)
        {
            return TypeGuidToIdMap.TryGetValue(entityType, out var guidMap) ? guidMap : null;
        }

        public IReadOnlySet<Guid>? GetGuidsForLibraryAndType(Guid libraryId, EntityType entityType)
        {
            if (LibraryTypeGuidsMap.TryGetValue(libraryId, out var typeGuidsMap))
            {
                return typeGuidsMap.TryGetValue(entityType, out var guids) ? guids : null;
            }
            return null;
        }

        public IReadOnlyDictionary<EntityType, IReadOnlySet<Guid>>? GetAllTypesForLibrary(Guid libraryId)
        {
            return LibraryTypeGuidsMap.TryGetValue(libraryId, out var typeGuidsMap) ? typeGuidsMap : null;
        }
    }
}
