using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using ThreatFramework.Infrastructure.Configuration;
using YamlDotNet.Serialization;
using Microsoft.Extensions.Options;
using ThreatFramework.Infra.Contract.Index;
using ThreatFramework.Core.IndexModel;

namespace ThreatFramework.Infrastructure.Services
{
    public class IndexService : IIndexService
    {
        private readonly ILogger<IndexService> _logger;
        private readonly ThreatModelingOptions _options;
        private readonly IDeserializer _yamlDeserializer;
        
        // High-performance lookup dictionaries - optimized for read operations
        private volatile ConcurrentDictionary<Guid, IndexItem> _guidToItemMap = new();
        private volatile ConcurrentDictionary<Guid, int> _guidToIdMap = new();
        private volatile ConcurrentDictionary<string, ConcurrentDictionary<Guid, int>> _kindGuidToIdMap = new();
        private volatile ConcurrentDictionary<string, List<IndexItem>> _kindToItemsMap = new();
        
        // Enum-based optimized dictionaries for minimal memory usage
        private volatile ConcurrentDictionary<EntityKind, ConcurrentDictionary<Guid, int>> _entityKindGuidToIdMap = new();
        private volatile ConcurrentDictionary<EntityKind, IndexItem[]> _entityKindToItemsMap = new();
        
        private DateTime _lastRefreshed = DateTime.MinValue;
        private readonly object _refreshLock = new object();

        public IndexService(
            ILogger<IndexService> logger, 
            IOptions<ThreatModelingOptions> options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            
            _yamlDeserializer = new DeserializerBuilder()
                .IgnoreUnmatchedProperties()
                .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.CamelCaseNamingConvention.Instance)
                .Build();

            // Initialize the cache at startup
            _ = Task.Run(async () => await RefreshAsync());
        }

        public int? GetIdByGuid(Guid guid)
        {
            return _guidToIdMap.TryGetValue(guid, out var id) ? id : null;
        }

        public int? GetIdByKindAndGuid(string kind, Guid guid)
        {
            if (string.IsNullOrWhiteSpace(kind))
                return null;

            return _kindGuidToIdMap.TryGetValue(kind.ToLowerInvariant(), out var kindMap) && 
                   kindMap.TryGetValue(guid, out var id) ? id : null;
        }

        public int? GetIdByKindAndGuid(EntityKind kind, Guid guid)
        {
            return _entityKindGuidToIdMap.TryGetValue(kind, out var kindMap) && 
                   kindMap.TryGetValue(guid, out var id) ? id : null;
        }

        public IndexItem? GetItemByGuid(Guid guid)
        {
            return _guidToItemMap.TryGetValue(guid, out var item) ? item : null;
        }

        public IEnumerable<IndexItem> GetItemsByKind(string kind)
        {
            if (string.IsNullOrWhiteSpace(kind))
                return Enumerable.Empty<IndexItem>();

            return _kindToItemsMap.TryGetValue(kind.ToLowerInvariant(), out var items) 
                ? items.AsReadOnly() 
                : Enumerable.Empty<IndexItem>();
        }

        public IEnumerable<IndexItem> GetItemsByKind(EntityKind kind)
        {
            return _entityKindToItemsMap.TryGetValue(kind, out var items) 
                ? items 
                : Enumerable.Empty<IndexItem>();
        }

        public async Task<bool> RefreshAsync()
        {
            try
            {
                // Use lock to prevent multiple simultaneous refreshes
                lock (_refreshLock)
                {
                    return RefreshInternal();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh index cache from YAML file: {FilePath}", _options.IndexYamlPath);
                return false;
            }
        }

        private bool RefreshInternal()
        {
            try
            {
                var yamlPath = GetResolvedYamlPath();
                
                if (!File.Exists(yamlPath))
                {
                    _logger.LogError("YAML file not found at path: {FilePath}", yamlPath);
                    return false;
                }

                var yamlContent = File.ReadAllText(yamlPath);
                _logger.LogInformation("YAML file content length: {ContentLength} characters", yamlContent.Length);
                
                var indexData = _yamlDeserializer.Deserialize<IndexData>(yamlContent);
                
                if (indexData == null)
                {
                    _logger.LogError("Failed to deserialize YAML data - indexData is null");
                    return false;
                }
                
                if (indexData.items == null)
                {
                    _logger.LogError("Failed to deserialize YAML data - items collection is null");
                    return false;
                }
                
                _logger.LogInformation("Deserialized YAML successfully. Items count: {ItemCount}", indexData.items.Count);

                if (indexData.items.Count == 0)
                {
                    _logger.LogWarning("YAML deserialized successfully but contains 0 items");
                    return false;
                }

                // Build new lookup dictionaries
                var newGuidToItemMap = new ConcurrentDictionary<Guid, IndexItem>();
                var newGuidToIdMap = new ConcurrentDictionary<Guid, int>();
                var newKindGuidToIdMap = new ConcurrentDictionary<string, ConcurrentDictionary<Guid, int>>();
                var newKindToItemsMap = new ConcurrentDictionary<string, List<IndexItem>>();
                var newEntityKindGuidToIdMap = new ConcurrentDictionary<EntityKind, ConcurrentDictionary<Guid, int>>();
                var entityKindItemsTemp = new Dictionary<EntityKind, List<IndexItem>>();

                int processedCount = 0;
                int skippedCount = 0;

                foreach (var item in indexData.items)
                {
                    if (item == null)
                    {
                        _logger.LogWarning("Skipping null item");
                        skippedCount++;
                        continue;
                    }
                    
                    if (item.Guid == Guid.Empty)
                    {
                        _logger.LogWarning("Skipping item with empty GUID: {Name}", item.Name);
                        skippedCount++;
                        continue;
                    }

                    // GUID to Item mapping
                    newGuidToItemMap[item.Guid] = item;
                    
                    // GUID to ID mapping
                    newGuidToIdMap[item.Guid] = item.Id;

                    // String-based mappings (for backward compatibility)
                    var normalizedKind = item.Kind?.ToLowerInvariant() ?? string.Empty;
                    if (!newKindGuidToIdMap.ContainsKey(normalizedKind))
                    {
                        newKindGuidToIdMap[normalizedKind] = new ConcurrentDictionary<Guid, int>();
                    }
                    newKindGuidToIdMap[normalizedKind][item.Guid] = item.Id;

                    if (!newKindToItemsMap.ContainsKey(normalizedKind))
                    {
                        newKindToItemsMap[normalizedKind] = new List<IndexItem>();
                    }
                    newKindToItemsMap[normalizedKind].Add(item);

                    // Enum-based mappings (optimized)
                    if (Enum.TryParse<EntityKind>(item.Kind, true, out var entityKind))
                    {
                        if (!newEntityKindGuidToIdMap.ContainsKey(entityKind))
                        {
                            newEntityKindGuidToIdMap[entityKind] = new ConcurrentDictionary<Guid, int>();
                        }
                        newEntityKindGuidToIdMap[entityKind][item.Guid] = item.Id;

                        if (!entityKindItemsTemp.ContainsKey(entityKind))
                        {
                            entityKindItemsTemp[entityKind] = new List<IndexItem>();
                        }
                        entityKindItemsTemp[entityKind].Add(item);
                    }
                    
                    processedCount++;
                }

                // Convert lists to arrays for minimal memory footprint
                var newEntityKindToItemsMap = new ConcurrentDictionary<EntityKind, IndexItem[]>();
                foreach (var kvp in entityKindItemsTemp)
                {
                    newEntityKindToItemsMap[kvp.Key] = kvp.Value.ToArray();
                }

                _logger.LogInformation("Processing complete. Processed: {ProcessedCount}, Skipped: {SkippedCount}", processedCount, skippedCount);

                // Atomically replace the old dictionaries with new ones
                _guidToItemMap = newGuidToItemMap;
                _guidToIdMap = newGuidToIdMap;
                _kindGuidToIdMap = newKindGuidToIdMap;
                _kindToItemsMap = newKindToItemsMap;
                _entityKindGuidToIdMap = newEntityKindGuidToIdMap;
                _entityKindToItemsMap = newEntityKindToItemsMap;
                _lastRefreshed = DateTime.UtcNow;

                _logger.LogInformation("Successfully refreshed index cache with {ItemCount} items from {FilePath}", 
                    processedCount, yamlPath);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during internal refresh operation");
                return false;
            }
        }

        public IndexCacheStatistics GetCacheStatistics()
        {
            var stats = new IndexCacheStatistics
            {
                TotalItems = _guidToItemMap.Count,
                LastRefreshed = _lastRefreshed,
                YamlFilePath = GetResolvedYamlPath()
            };

            foreach (var kvp in _entityKindToItemsMap)
            {
                stats.ItemsByKind[kvp.Key] = kvp.Value.Length;
            }

            return stats;
        }

        private string GetResolvedYamlPath()
        {
            if (Path.IsPathRooted(_options.IndexYamlPath))
            {
                return _options.IndexYamlPath;
            }

            // Resolve relative path from the application's base directory
            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            return Path.GetFullPath(Path.Combine(basePath, _options.IndexYamlPath));
        }
        }
}