using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThreatFramework.Infra.Contract;
using ThreatFramework.Infrastructure.Interfaces.Repositories;

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
            await EnsureCacheInitializedAsync();
            return new Dictionary<int, Guid>(_idToGuidCache);
        }

        public async Task<HashSet<Guid>> GetReadonlyLibraryGuidsAsync()
        {
            await EnsureCacheInitializedAsync();
            return _readonlyStatusCache
                .Where(kvp => kvp.Value)
                .Select(kvp => kvp.Key)
                .ToHashSet();
        }

        public async Task<Guid> GetGuidByIdAsync(int libraryId)
        {
            await EnsureCacheInitializedAsync();
            return _idToGuidCache.TryGetValue(libraryId, out var guid) ? guid : null;
        }

        public async Task<bool> IsLibraryReadonlyAsync(Guid libraryGuid)
        {
            await EnsureCacheInitializedAsync();
            return _readonlyStatusCache.TryGetValue(libraryGuid, out var isReadonly) && isReadonly;
        }

        public async Task RefreshCacheAsync()
        {
            var libraries = await _libraryRepository.GetLibrariesCacheAsync();
            
            _idToGuidCache.Clear();
            _readonlyStatusCache.Clear();

            foreach (var library in libraries)
            {
                _idToGuidCache.TryAdd(library.Id, library.Guid);
                _readonlyStatusCache.TryAdd(library.Guid, library.IsReadonly);
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

        private async Task EnsureCacheInitializedAsync()
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
            await EnsureCacheInitializedAsync();
            
            var readonlyGuids = _readonlyStatusCache
                .Where(kvp => kvp.Value)
                .Select(kvp => kvp.Key);
            
            var readonlyIds = _idToGuidCache
                .Where(kvp => readonlyGuids.Contains(kvp.Value))
                .Select(kvp => kvp.Key)
                .ToHashSet();
            
            return readonlyIds;
        }

        public async Task<HashSet<int>> GetIdsFromGuid(IEnumerable<Guid> guids)
        {
            await EnsureCacheInitializedAsync();
            
            var ids = new HashSet<int>();
            
            foreach (var guid in guids)
            {
                var id = _idToGuidCache.FirstOrDefault(kvp => kvp.Value == guid).Key;
                if (id != 0) // Default value for int when not found
                {
                    ids.Add(id);
                }
            }
            
            return ids;
        }
    }
}
