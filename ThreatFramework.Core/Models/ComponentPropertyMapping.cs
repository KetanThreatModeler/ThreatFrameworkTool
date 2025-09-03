namespace ThreatFramework.Core.Models
{
    public class ComponentPropertyMapping
    {
        public int Id { get; set; }
        public int ComponentId { get; set; }
        public int PropertyId { get; set; }
        public bool IsOptional { get; set; }
        public bool IsHidden { get; set; }
        public bool IsOverridden { get; set; }
    }
}
