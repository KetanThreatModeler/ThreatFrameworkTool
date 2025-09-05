namespace ThreatFramework.Core.Models.ComponentMapping
{
    public class ComponentSecurityRequirementMapping
    {
        public Guid SecurityRequirementId { get; set; }
        public Guid ComponentId { get; set; }
        public bool IsHidden { get; set; }
        public bool IsOverridden { get; set; }
    }
}
