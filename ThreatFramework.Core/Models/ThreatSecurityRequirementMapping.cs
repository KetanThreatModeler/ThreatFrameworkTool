namespace ThreatFramework.Core.Models
{
    public class ThreatSecurityRequirementMapping
    {
        public int SecurityRequirementId { get; set; }
        public int ThreatId { get; set; }
        public bool IsHidden { get; set; }
        public bool IsOverridden { get; set; }
    }
}
