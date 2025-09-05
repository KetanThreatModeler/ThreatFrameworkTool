namespace ThreatFramework.Core.Models.Cache
{
    public class LibraryCache
    {
        public int Id { get; set; }
        public Guid Guid { get; set; }
        public bool IsReadonly { get; set; }
    }
}
