namespace ThreatFramework.Core.Models.ComponentMapping
{
    public class ComponentThreatSecurityRequirementMapping
    {
        public int Id { get; set; }
        public int ComponentThreatId { get; set; }
        public int SecurityRequirementId { get; set; }
        public bool IsHidden { get; set; }
        public bool IsOverridden { get; set; }
    }
}
