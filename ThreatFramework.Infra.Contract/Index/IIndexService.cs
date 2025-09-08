using ThreatFramework.Core;
using ThreatFramework.Core.IndexModel;

namespace ThreatFramework.Infra.Contract.Index
{
    public interface IIndexService
    {
      
        int? GetIdByGuid(Guid guid);

        int? GetIdByKindAndGuid(EntityKind kind, Guid guid);

       
        IndexItem? GetItemByGuid(Guid guid);

        
        IEnumerable<IndexItem> GetItemsByKind(EntityKind kind);

       
        Task<bool> RefreshAsync();

      
        IndexCacheStatistics GetCacheStatistics();
    }

    public class IndexCacheStatistics
    {
        public int TotalItems { get; set; }
        public Dictionary<EntityKind, int> ItemsByKind { get; set; } = new();
        public DateTime LastRefreshed { get; set; }
        public string YamlFilePath { get; set; } = string.Empty;
    }
}