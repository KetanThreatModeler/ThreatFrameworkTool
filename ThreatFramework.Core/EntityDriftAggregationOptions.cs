namespace ThreatFramework.Core
{
    public sealed class EntityDriftAggregationOptions
    {
        public List<string> LibraryDefaultFields { get; init; } = new List<string>
        {
            "IsDefault",
            "Sharingtype",
            "ReleaseNotes",
            "ImageUrl",
            "Name",
            "Version",
            "Description",
            "Readonly"
        };

        public List<string> ThreatDefaultFields { get; init; } = new List<string>
        {
            "RiskName",
            "LibraryGuid",
            "Automated",
            "IsHidden",
            "Name",
            "ChineseName",
            "Labels",
            "Description",
            "Reference",
            "Intelligence",
            "ChineseDescription"
        };

        public List<string> ComponentDefaultFields { get; init; } = new List<string>
        {
            "LibraryGuid",
            "ComponentTypeGuid",
            "IsHidden",
            "IsOverridden",
            "Name",
            "ImagePath",
            "Labels",
            "Version",
            "Description",
            "ChineseDescription"
        };
        public List<string> SecurityRequirementDefaultFields { get; init; } = new List<string>
        {
            "RiskName",
            "LibraryId",
            "IsCompensatingControl",
            "IsHidden",
            "Guid",
            "Name",
            "ChineseName",
            "Labels",
            "Description",
            "ChineseDescription"
        };

        public List<string> TestCaseDefaultFields { get; init; } = new List<string> {
            "LibraryId",
            "IsHidden",
            "Guid",
            "Name",
            "ChineseName",
            "Labels",
            "Description",
            "ChineseDescription"
        };


        public List<string> PropertyDefaultFields { get; init; } = new List<string> {
        "LibraryGuid",
        "IsSelected",
        "IsOptional",
        "IsGlobal",
        "IsHidden",
        "Guid",
        "Name",
        "ChineseName",
        "Labels",
        "Description",
        "ChineseDescription" };

        public List<string> PropertyOptionDefaultFields { get; init; } = new List<string> {
            "IsDefault",
            "IsHidden",
            "IsOverridden",
            "OptionText",
            "ChineseOptionText"
        };

        public List<string> PropertyTypeDefaultFields { get; init; } = new List<string> {
            "Guid",
            "Name"
        };

        public List<string> ComponentTypeDefaultFields { get; init; } = new List<string> {
            "Name",
            "Description",
            "ChineseName",
            "ChineseDescription",
            "IsHidden",
            "IsSecurityControl"
        };
        public List<string> RelationshipDefaultFields { get; init; } = new List<string>
        {
            "RelationshipName",
            "Description",
            "ChineseRelationship"
        };

        public List<string> ResourceTypeValuesDefaultFields { get; init; } = new List<string>
        {
            "ResourceName",
            "ResourceTypeValue",
            "ComponentGuid"
        };

        public List<string> ResourceTypeValueRelationshipDefaultFields { get; init; } = new List<string>
        {
            "IsRequired",
            "IsDeleted"
        };
    }
}
