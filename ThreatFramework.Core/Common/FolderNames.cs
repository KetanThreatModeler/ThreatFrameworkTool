using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatModeler.TF.Git.Contract.Common
{
    public static class FolderNames
    {
        // Library folders
        public const string Components = "components";
        public const string SecurityRequirements = "security-requirements";
        public const string TestCases = "testcases";
        public const string Threats = "threats";
        public const string Properties = "properties";

        // Global folders
        public const string ComponentType = "component-type";
        public const string PropertyType = "property-type";
        public const string PropertyOptions = "property-options";

        // Mapping folders
        public const string ComponentProperty = "component-property";
        public const string ComponentPropertyOption = "component-property-option";
        public const string ComponentPropertyOptionThreat = "component-property-option-threat";
        public const string ComponentPropertyOptionThreatSecurityRequirement =
            "component-property-option-threat-security-requirement";
        public const string ComponentThreat = "component-threat";
        public const string ComponentThreatSecurityRequirement = "component-threat-security-requirement";
        public const string ComponentSecurityRequirement = "component-security-requirement";
        public const string ThreatSecurityRequirement = "threat-security-requirement";
        
        public const string Relationships = "relationships";
        public const string ResourceTypeValues = "resourcetypevalues"; // corrected back to original casing
        public const string ResourceValueTypeRelationship = "resourcevaluetyperelationship"; // corrected back to original casing

    }
}
