using System.Collections.Concurrent;
using ThreatFramework.Infra.Contract;
using ThreatFramework.Infra.Contract.Repository;

namespace ThreatFramework.Infrastructure.Services
{
    public class LibraryCacheService : ILibraryCacheService
    {
        private readonly ILibraryRepository _libraryRepository;
        private readonly ConcurrentDictionary<int, Guid> _idToGuidCache = new();
        private readonly ConcurrentDictionary<Guid, bool> _readonlyStatusCache = new();
        private volatile bool _cacheInitialized = false;
        private readonly object _lockObject = new();

        public LibraryCacheService(ILibraryRepository libraryRepository)
        {
            _libraryRepository = libraryRepository ?? throw new ArgumentNullException(nameof(libraryRepository));
        }

        public async Task<Dictionary<int, Guid>> GetIdToGuidLookupAsync()
        {
            EnsureCacheInitialized();
            return new Dictionary<int, Guid>(_idToGuidCache);
        }

        public async Task<HashSet<Guid>> GetReadonlyLibraryGuidsAsync()
        {
            EnsureCacheInitialized();
            return _readonlyStatusCache
                .Where(kvp => kvp.Value)
                .Select(kvp => kvp.Key)
                .ToHashSet();
        }

        public async Task<Guid> GetGuidByIdAsync(int libraryId)
        {
            EnsureCacheInitialized();
            return _idToGuidCache.TryGetValue(libraryId, out Guid guid) ? guid : Guid.Empty;
        }

        public async Task<bool> IsLibraryReadonlyAsync(Guid libraryGuid)
        {
            EnsureCacheInitialized();
            return _readonlyStatusCache.TryGetValue(libraryGuid, out bool isReadonly) && isReadonly;
        }

        public async Task RefreshCacheAsync()
        {
            IEnumerable<Core.Cache.LibraryCache> libraries = await _libraryRepository.GetLibrariesCacheAsync();

            _idToGuidCache.Clear();
            _readonlyStatusCache.Clear();

            foreach (Core.Cache.LibraryCache library in libraries)
            {
                _ = _idToGuidCache.TryAdd(library.Id, library.Guid);
                _ = _readonlyStatusCache.TryAdd(library.Guid, library.IsReadonly);
            }

            _cacheInitialized = true;
        }

        public Task ClearCacheAsync()
        {
            _idToGuidCache.Clear();
            _readonlyStatusCache.Clear();
            _cacheInitialized = false;
            return Task.CompletedTask;
        }

        private void EnsureCacheInitialized()
        {
            if (!_cacheInitialized)
            {
                lock (_lockObject)
                {
                    if (!_cacheInitialized)
                    {
                        RefreshCacheAsync().Wait();
                    }
                }
            }
        }

        public async Task<HashSet<int>> GetReadOnlyLibraryIdAsync()
        {
            EnsureCacheInitialized();

            IEnumerable<Guid> readonlyGuids = _readonlyStatusCache
                .Where(kvp => kvp.Value)
                .Select(kvp => kvp.Key);

            HashSet<int> readonlyIds = _idToGuidCache
                .Where(kvp => readonlyGuids.Contains(kvp.Value))
                .Select(kvp => kvp.Key)
                .ToHashSet();

            return readonlyIds;
        }

        public async Task<HashSet<int>> GetIdsFromGuid(IEnumerable<Guid> guids)
        {
            EnsureCacheInitialized();

            HashSet<int> ids = [];

            foreach (Guid guid in guids)
            {
                int id = _idToGuidCache.FirstOrDefault(kvp => kvp.Value == guid).Key;
                if (id != 0) // Default value for int when not found
                {
                    _ = ids.Add(id);
                }
            }

            return ids;
        }
    }
}
