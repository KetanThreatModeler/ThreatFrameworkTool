using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Core;
using ThreatFramework.Core.CoreEntities;
using ThreatFramework.Drift.Contract.MappingDriftService.Model;
using ThreatFramework.Drift.Contract.Model;
using ThreatModeler.TF.Core.Global;
using ThreatModeler.TF.Drift.Contract.MappingDriftService.Dto;

namespace ThreatModeler.TF.Drift.Contract.Model.UpdatedFinal
{
    public class ComponentDrift1
    {
        public List<AddedComponent1> Added { get; init; } = new();
        public List<DeletedComponent1> Deleted { get; init; } = new();
        public List<ModifiedComponent1> Modified { get; init; } = new();
    }

    public class ComponentMappingCollection1
    {
        public List<SecurityRequirement> SecurityRequirements { get; set; } = new();
        public List<ThreatSRMappingDto1> ThreatSRMappings { get; set; } = new();
        public List<PropertyThreatSRMappingDto1> PropertyThreatSRMappings { get; set; } = new();
    }

    public class AddedComponent1
    {
        public Component Component { get; set; } = null!;

        // All mappings for this added component (implicitly "added" mappings)
        public ComponentMappingCollection1 Mappings { get; set; } = new();
    }

    public class DeletedComponent1
    {
        public Component Component { get; set; } = null!;

        // All mappings for this deleted component (implicitly "deleted" mappings)
        public ComponentMappingCollection1 Mappings { get; set; } = new();
    }

    public class ModifiedComponent1
    {
        public Component Component { get; set; } = null!;

        // Reuse existing ModifiedEntity structure for field changes
        public List<FieldChange> ChangedFields { get; set; } = new();

        // Granular mapping changes for this modified component
        public ComponentMappingCollection1 MappingsAdded { get; set; } = new();
        public ComponentMappingCollection1 MappingsRemoved { get; set; } = new();
    }

    public class ThreatSRMappingDto1
    {
       public Threat threat { get; set; } = null!;
       public List<SecurityRequirement> securityRequirements { get; set; } = new();
    }

    public class PropertyThreatSRMappingDto1
    {
       public Property Property { get; set; } = null!;
      public List<PropertyOptionThreatSRMappingDto1> PropertyOptionThreatSRMapping { get; set; } = new();
    }

    public class PropertyOptionThreatSRMappingDto1
    {
       public PropertyOption PropertyOption { get; set; } = null!;
       public List<ThreatSRMappingDto1> ThreatSRMappings { get; set; } = new();
    }
}
