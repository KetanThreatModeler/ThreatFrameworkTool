using ThreatFramework.Core;
using ThreatModeler.TF.Core.Model.CoreEntities;
using ThreatModeler.TF.Core.Model.Global;

namespace ThreatModeler.TF.Drift.Contract.Model
{
    public class ComponentDrift
    {
        public List<AddedComponent> Added { get; init; } = new();
        public List<DeletedComponent> Removed { get; init; } = new();
        public List<ModifiedComponent> Modified { get; init; } = new();
    }

    public class ComponentMappingCollection
    {
        public List<SecurityRequirement> SecurityRequirements { get; set; } = new();
        public List<ThreatSRMapping> ThreatSRMappings { get; set; } = new();
        public List<PropertyThreatSRMapping> PropertyThreatSRMappings { get; set; } = new();
    }

    public class AddedComponent
    {
        public Component Component { get; set; } = null!;

        public ComponentMappingCollection Mappings { get; set; } = new();
    }

    public class DeletedComponent
    {
        public Component Component { get; set; } = null!;

        public ComponentMappingCollection Mappings { get; set; } = new();
    }

    public class ModifiedComponent
    {
        public Component Component { get; set; } = null!;

        public List<FieldChange> ChangedFields { get; set; } = new();

        public ComponentMappingCollection MappingsAdded { get; set; } = new();
        public ComponentMappingCollection MappingsRemoved { get; set; } = new();
    }

    public class ThreatSRMapping
    {
       public Threat threat { get; set; } = null!;
       public List<SecurityRequirement> securityRequirements { get; set; } = new();
    }

    public class PropertyThreatSRMapping
    {
       public Property Property { get; set; } = null!;
      public List<PropertyOptionThreatSRMapping> PropertyOptionThreatSRMapping { get; set; } = new();
    }

    public class PropertyOptionThreatSRMapping
    {
       public PropertyOption PropertyOption { get; set; } = null!;
       public List<ThreatSRMapping> ThreatSRMappings { get; set; } = new();
    }
}
