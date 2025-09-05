using ThreatFramework.Core.Models.IndexModel;

namespace ThreatFramework.Infra.Contract.Index
{
    public interface IIndexService
    {
        /// <summary>
        /// Gets an ID by GUID lookup
        /// </summary>
        /// <param name="guid">The GUID to lookup</param>
        /// <returns>The corresponding ID, or null if not found</returns>
        int? GetIdByGuid(Guid guid);

        /// <summary>
        /// Gets an ID by kind and GUID combination
        /// </summary>
        /// <param name="kind">The kind of the item</param>
        /// <param name="guid">The GUID to lookup</param>
        /// <returns>The corresponding ID, or null if not found</returns>
        int? GetIdByKindAndGuid(string kind, Guid guid);

        /// <summary>
        /// Gets an item by GUID
        /// </summary>
        /// <param name="guid">The GUID to lookup</param>
        /// <returns>The IndexItem if found, null otherwise</returns>
        IndexItem? GetItemByGuid(Guid guid);

        /// <summary>
        /// Gets all items of a specific kind
        /// </summary>
        /// <param name="kind">The kind to filter by</param>
        /// <returns>Collection of items matching the kind</returns>
        IEnumerable<IndexItem> GetItemsByKind(string kind);

        /// <summary>
        /// Refreshes the in-memory cache from the YAML file
        /// </summary>
        /// <returns>True if refresh was successful, false otherwise</returns>
        Task<bool> RefreshAsync();

        /// <summary>
        /// Gets statistics about the current cache
        /// </summary>
        /// <returns>Statistics object with cache information</returns>
        IndexCacheStatistics GetCacheStatistics();
    }

    public class IndexCacheStatistics
    {
        public int TotalItems { get; set; }
        public Dictionary<string, int> ItemsByKind { get; set; } = new();
        public DateTime LastRefreshed { get; set; }
        public string YamlFilePath { get; set; } = string.Empty;
    }
}