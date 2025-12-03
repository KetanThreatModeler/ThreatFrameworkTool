using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ThreatFramework.Infra.Contract.Index;

namespace ThreatModeler.TF.Infra.Implmentation.Index
{
    public sealed class GuidIndexData
    {
        public IReadOnlyDictionary<EntityType, IReadOnlyDictionary<Guid, int>> TypeGuidToIdMap { get; }
        public IReadOnlyDictionary<Guid, IReadOnlyDictionary<EntityType, IReadOnlySet<Guid>>> LibraryTypeGuidsMap { get; }
        public int MaxId { get; }
        public int Count { get; }

        private readonly IReadOnlyDictionary<int, Guid> _idToGuidMap;

        /// <summary>
        /// Main constructor: builds all maps from GuidIndex objects.
        /// </summary>
        public GuidIndexData(IEnumerable<GuidIndex> indices)
        {
            if (indices is null) throw new ArgumentNullException(nameof(indices));

            // Use local mutable structures as builders
            var typeGuidToId = new Dictionary<EntityType, Dictionary<Guid, int>>();
            var libraryTypeGuids = new Dictionary<Guid, Dictionary<EntityType, HashSet<Guid>>>();
            var idToGuid = new Dictionary<int, Guid>();

            var maxId = 0;
            var totalCount = 0;

            foreach (var index in indices)
            {
                if (index is null)
                {
                    continue;
                }

                AddTypeGuidToIdMapping(typeGuidToId, index.EntityType, index.Guid, index.Id);
                AddLibraryTypeGuidMapping(libraryTypeGuids, index.LibraryGuid, index.EntityType, index.Guid);

                idToGuid[index.Id] = index.Guid;

                totalCount++;
                if (index.Id > maxId)
                {
                    maxId = index.Id;
                }
            }

            TypeGuidToIdMap = ToReadOnly(typeGuidToId);
            LibraryTypeGuidsMap = ToReadOnly(libraryTypeGuids);
            _idToGuidMap = new ReadOnlyDictionary<int, Guid>(idToGuid);

            MaxId = maxId;
            Count = totalCount;
        }

        /// <summary>
        /// Alternate factory to build from EntityIdentifier + existing guid->id map.
        /// </summary>
        public static GuidIndexData FromEntities(
            IReadOnlyDictionary<Guid, int> guidToIdMap,
            IEnumerable<EntityIdentifier> entities)
        {
            if (guidToIdMap is null) throw new ArgumentNullException(nameof(guidToIdMap));
            if (entities is null) throw new ArgumentNullException(nameof(entities));

            var typeGuidToId = new Dictionary<EntityType, Dictionary<Guid, int>>();
            var libraryTypeGuids = new Dictionary<Guid, Dictionary<EntityType, HashSet<Guid>>>();
            var idToGuid = new Dictionary<int, Guid>();

            var maxId = 0;
            var totalCount = 0;

            foreach (var entity in entities)
            {
                if (!guidToIdMap.TryGetValue(entity.Guid, out var id))
                {
                    throw new KeyNotFoundException(
                        $"No Id mapping found for Guid {entity.Guid} while building {nameof(GuidIndexData)}.");
                }

                AddTypeGuidToIdMapping(typeGuidToId, entity.EntityType, entity.Guid, id);
                AddLibraryTypeGuidMapping(libraryTypeGuids, entity.LibraryGuid, entity.EntityType, entity.Guid);

                idToGuid[id] = entity.Guid;

                totalCount++;
                if (id > maxId)
                {
                    maxId = id;
                }
            }

            return new GuidIndexData(
                ToReadOnly(typeGuidToId),
                ToReadOnly(libraryTypeGuids),
                new ReadOnlyDictionary<int, Guid>(idToGuid),
                maxId,
                totalCount);
        }

        // Private "core" constructor used by FromEntities
        private GuidIndexData(
            IReadOnlyDictionary<EntityType, IReadOnlyDictionary<Guid, int>> typeGuidToIdMap,
            IReadOnlyDictionary<Guid, IReadOnlyDictionary<EntityType, IReadOnlySet<Guid>>> libraryTypeGuidsMap,
            IReadOnlyDictionary<int, Guid> idToGuidMap,
            int maxId,
            int count)
        {
            TypeGuidToIdMap = typeGuidToIdMap ?? throw new ArgumentNullException(nameof(typeGuidToIdMap));
            LibraryTypeGuidsMap = libraryTypeGuidsMap ?? throw new ArgumentNullException(nameof(libraryTypeGuidsMap));
            _idToGuidMap = idToGuidMap ?? throw new ArgumentNullException(nameof(idToGuidMap));

            MaxId = maxId;
            Count = count;
        }

        #region Public API

        /// <summary>
        /// Tries to get an ID for the given GUID, regardless of entity type.
        /// </summary>
        public bool TryGetId(Guid guid, out int id)
        {
            foreach (var typeMap in TypeGuidToIdMap.Values)
            {
                if (typeMap.TryGetValue(guid, out id))
                {
                    return true;
                }
            }

            id = 0;
            return false;
        }

        /// <summary>
        /// Tries to get an ID for the given GUID and entity type.
        /// </summary>
        public bool TryGetId(EntityType entityType, Guid guid, out int id)
        {
            if (TypeGuidToIdMap.TryGetValue(entityType, out var guidMap))
            {
                return guidMap.TryGetValue(guid, out id);
            }

            id = 0;
            return false;
        }

        /// <summary>
        /// Returns the GUID->ID map for a specific entity type, or null if none.
        /// </summary>
        public IReadOnlyDictionary<Guid, int>? GetGuidsForType(EntityType entityType) =>
            TypeGuidToIdMap.TryGetValue(entityType, out var guidMap) ? guidMap : null;

        /// <summary>
        /// Returns all GUIDs for a given library and entity type, or null if none.
        /// </summary>
        public IReadOnlySet<Guid>? GetGuidsForLibraryAndType(Guid libraryId, EntityType entityType)
        {
            return LibraryTypeGuidsMap.TryGetValue(libraryId, out var typeGuidsMap)
                && typeGuidsMap.TryGetValue(entityType, out var guids)
                ? guids
                : null;
        }

        /// <summary>
        /// Returns all entity types and their GUID sets for a given library, or null if library is not present.
        /// </summary>
        public IReadOnlyDictionary<EntityType, IReadOnlySet<Guid>>? GetAllTypesForLibrary(Guid libraryId) =>
            LibraryTypeGuidsMap.TryGetValue(libraryId, out var typeGuidsMap) ? typeGuidsMap : null;

        /// <summary>
        /// Returns all integer IDs for entities belonging to a given library and entity type.
        /// </summary>
        public IReadOnlyCollection<int> GetIdsForLibraryAndType(Guid libraryId, EntityType entityType)
        {
            // 1) Get the set of GUIDs for this library + type
            if (!LibraryTypeGuidsMap.TryGetValue(libraryId, out var typeGuidsMap) ||
                !typeGuidsMap.TryGetValue(entityType, out var guids) ||
                guids is null ||
                guids.Count == 0)
            {
                return Array.Empty<int>();
            }

            // 2) Resolve GUIDs to IDs via the type-level map
            if (!TypeGuidToIdMap.TryGetValue(entityType, out var guidToIdMap))
            {
                return Array.Empty<int>();
            }

            var result = new List<int>(guids.Count);

            foreach (var guid in guids)
            {
                if (guidToIdMap.TryGetValue(guid, out var id))
                {
                    result.Add(id);
                }
            }

            if (result.Count == 0)
            {
                return Array.Empty<int>();
            }

            result.Sort();
            return result.ToArray();
        }

        /// <summary>
        /// Tries to resolve an ID back to its Guid.
        /// </summary>
        public bool TryGetGuid(int id, out Guid guid)
        {
            if (_idToGuidMap.TryGetValue(id, out guid))
            {
                return true;
            }

            guid = Guid.Empty;
            return false;
        }

        #endregion

        #region Private helpers (normalization / map construction)

        private static void AddTypeGuidToIdMapping(
            IDictionary<EntityType, Dictionary<Guid, int>> map,
            EntityType entityType,
            Guid guid,
            int id)
        {
            if (!map.TryGetValue(entityType, out var guidMap))
            {
                guidMap = [];
                map[entityType] = guidMap;
            }

            guidMap[guid] = id;
        }

        private static void AddLibraryTypeGuidMapping(
            IDictionary<Guid, Dictionary<EntityType, HashSet<Guid>>> map,
            Guid libraryGuid,
            EntityType entityType,
            Guid guid)
        {
            if (!map.TryGetValue(libraryGuid, out var typeMap))
            {
                typeMap = [];
                map[libraryGuid] = typeMap;
            }

            if (!typeMap.TryGetValue(entityType, out var guids))
            {
                guids = [];
                typeMap[entityType] = guids;
            }

            guids.Add(guid);
        }

        private static IReadOnlyDictionary<EntityType, IReadOnlyDictionary<Guid, int>> ToReadOnly(
            IDictionary<EntityType, Dictionary<Guid, int>> source) =>
            source.ToDictionary(
                kvp => kvp.Key,
                kvp => (IReadOnlyDictionary<Guid, int>)
                    new ReadOnlyDictionary<Guid, int>(kvp.Value));

        private static IReadOnlyDictionary<Guid, IReadOnlyDictionary<EntityType, IReadOnlySet<Guid>>> ToReadOnly(
            IDictionary<Guid, Dictionary<EntityType, HashSet<Guid>>> source) =>
            source.ToDictionary(
                kvp => kvp.Key,
                kvp => (IReadOnlyDictionary<EntityType, IReadOnlySet<Guid>>)
                    new ReadOnlyDictionary<EntityType, IReadOnlySet<Guid>>(
                        kvp.Value.ToDictionary(
                            inner => inner.Key,
                            inner => (IReadOnlySet<Guid>)inner.Value)));

        #endregion
    }
}
