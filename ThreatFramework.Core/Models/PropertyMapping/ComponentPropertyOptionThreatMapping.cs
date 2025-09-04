namespace ThreatFramework.Core.Models.PropertyMapping
{
    public class ComponentPropertyOptionThreatMapping

    {
        public int Id { get; set; }
        public int ComponentPropertyOptionId { get; set; }
        public int ThreatId { get; set; }
        public bool IsHidden { get; set; }
        public bool IsOverridden { get; set; }
    }
}
