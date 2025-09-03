namespace ThreatFramework.Core.Models
{
    public class ComponentSecurityRequirementMapping
    {
        public int SecurityRequirementId { get; set; }
        public int ComponentId { get; set; }
        public bool IsHidden { get; set; }
        public bool IsOverridden { get; set; }
    }
}
