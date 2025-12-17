using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatModeler.TF.Git.Contract.Models
{
    public enum DomainEntityType
    {
        Unknown = 0,

        Library,
        // Library-specific
        Components,
        SecurityRequirements,
        TestCases,
        Threats,
        Properties,

        // Global
        ComponentType,
        PropertyType,
        PropertyOptions,

        // Mappings
        ComponentProperty,
        ComponentPropertyOptions,
        ComponentPropertyOptionThreats,
        ComponentPropertyOptionThreatSecurityRequirements,
        ComponentThreat,
        ComponentThreatSecurityRequirements,
        ComponentSecurityRequirements,
        ThreatSecurityRequirements,

        Relationships,
        ResourceTypeValues,
        ResourceTypeValueRelationships,

    }
}
