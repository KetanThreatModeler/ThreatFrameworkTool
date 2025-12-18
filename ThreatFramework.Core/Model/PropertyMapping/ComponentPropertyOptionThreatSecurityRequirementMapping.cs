namespace ThreatModeler.TF.Core.Model.PropertyMapping
{
    public class ComponentPropertyOptionThreatSecurityRequirementMapping
    {
        public int Id { get; set; }
        public Guid ComponentGuid { get; set; }
        public Guid PropertyGuid { get; set; }
        public Guid PropertyOptionGuid { get; set; }
        public Guid ThreatGuid { get; set; }
        public Guid SecurityRequirementGuid { get; set; }
        public bool IsHidden { get; set; }
        public bool IsOverridden { get; set; }
    }
}
