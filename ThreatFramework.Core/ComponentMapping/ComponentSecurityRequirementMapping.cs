namespace ThreatFramework.Core.ComponentMapping
{
    public class ComponentSecurityRequirementMapping
    {
        public Guid SecurityRequirementGuid { get; set; }
        public Guid ComponentGuid { get; set; }
        public bool IsHidden { get; set; }
        public bool IsOverridden { get; set; }
    }
}
