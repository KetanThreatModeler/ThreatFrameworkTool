namespace ThreatFramework.Core.ComponentMapping
{
    public class ComponentThreatSecurityRequirementMapping
    {
        public Guid ComponentGuid { get; set; }
        public Guid ThreatGuid { get; set; }
        public Guid SecurityRequirementGuid { get; set; }
        public bool IsHidden { get; set; }
        public bool IsOverridden { get; set; }
    }
}
