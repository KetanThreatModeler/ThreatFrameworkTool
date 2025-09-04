namespace ThreatFramework.Core.Models.PropertyMapping
{
    public class ComponentPropertyOptionMapping
    {
        public int Id { get; set; }
        public int ComponentPropertyId { get; set; }
        public int PropertyOptionId { get; set; }
        public bool IsDefault { get; set; }
        public bool IsHidden { get; set; }
        public bool IsOverridden { get; set; }
    }
}
