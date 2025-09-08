namespace ThreatFramework.Core.IndexModel
{
    public class IndexItem
    {
        public string Kind { get; set; } = string.Empty;
        public Guid Guid { get; set; }
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
