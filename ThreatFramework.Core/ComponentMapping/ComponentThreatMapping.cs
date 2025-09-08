namespace ThreatFramework.Core.ComponentMapping
{
    public class ComponentThreatMapping
    {
        public int Id { get; set; }
        public Guid ThreatGuid { get; set; }
        public Guid ComponentGuid { get; set; }
        public bool IsHidden { get; set; }
        public bool IsOverridden { get; set; }
        public bool UsedForMitigation { get; set; }
    }
}
