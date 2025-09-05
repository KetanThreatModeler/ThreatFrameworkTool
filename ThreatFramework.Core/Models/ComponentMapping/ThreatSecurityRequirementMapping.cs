namespace ThreatFramework.Core.Models.ComponentMapping
{
    public class ThreatSecurityRequirementMapping
    {
        public Guid SecurityRequirementGuid { get; set; }
        public Guid ThreatGuid { get; set; }
        public bool IsHidden { get; set; }
        public bool IsOverridden { get; set; }
    }
}
