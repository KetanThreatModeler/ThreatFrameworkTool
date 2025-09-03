namespace ThreatFramework.Infrastructure.Models
{
    public class IndexItem
    {
        public string Kind { get; set; } = string.Empty;
        public Guid Guid { get; set; }
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class IndexData
    {
        public List<IndexItem> items { get; set; } = new();
    }
}
