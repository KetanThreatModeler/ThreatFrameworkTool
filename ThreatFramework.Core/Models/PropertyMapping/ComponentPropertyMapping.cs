namespace ThreatFramework.Core.Models.PropertyMapping
{
    public class ComponentPropertyMapping
    {
        public int Id { get; set; }
        public Guid ComponentGuid { get; set; }
        public Guid PropertyGuid { get; set; }
        public bool IsOptional { get; set; }
        public bool IsHidden { get; set; }
        public bool IsOverridden { get; set; }
    }
}
