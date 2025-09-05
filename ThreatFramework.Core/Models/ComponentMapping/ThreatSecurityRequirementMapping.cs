namespace ThreatFramework.Core.Models.ComponentMapping
{
    public class ThreatSecurityRequirementMapping
    {
        public Guid SecurityRequirementId { get; set; }
        public Guid ThreatId { get; set; }
        public bool IsHidden { get; set; }
        public bool IsOverridden { get; set; }
    }
}
