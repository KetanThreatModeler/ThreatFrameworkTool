namespace ThreatFramework.Core.Models
{
    public class ComponentThreatMapping
    {
        public int Id { get; set; }
        public int ThreatId { get; set; }
        public int ComponentId { get; set; }
        public bool IsHidden { get; set; }
        public bool IsOverridden { get; set; }
        public bool UsedForMitigation { get; set; }
    }
}
