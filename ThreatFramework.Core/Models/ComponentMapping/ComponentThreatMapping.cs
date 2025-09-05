namespace ThreatFramework.Core.Models.ComponentMapping
{
    public class ComponentThreatMapping
    {
        public int Id { get; set; }
        public Guid ThreatId { get; set; }
        public Guid ComponentId { get; set; }
        public bool IsHidden { get; set; }
        public bool IsOverridden { get; set; }
        public bool UsedForMitigation { get; set; }
    }
}
