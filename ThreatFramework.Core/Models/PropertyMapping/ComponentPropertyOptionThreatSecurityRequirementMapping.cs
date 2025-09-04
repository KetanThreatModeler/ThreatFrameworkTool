namespace ThreatFramework.Core.Models.PropertyMapping
{
    public class ComponentPropertyOptionThreatSecurityRequirementMapping
    {
        public int Id { get; set; }
        public int ComponentPropertyOptionThreatId { get; set; }
        public int SecurityRequirementId { get; set; }
        public bool IsHidden { get; set; }
        public bool IsOverridden { get; set; }
    }
}
