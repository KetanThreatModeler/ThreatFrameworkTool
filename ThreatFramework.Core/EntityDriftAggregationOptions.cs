using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatFramework.Core
{
    public sealed class EntityDriftAggregationOptions
    {
        public List<string> LibraryDefaultFields { get; init; } = new List<string>
        {
            "IsDefault",
            "sharingtype",
            "version",
            "ReleaseNotes",
            "ImageUrl",
            "Name",
            "Version",
            "Description"
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
            "PropertyId",
            "IsDefault",
            "IsHidden",
            "IsOverridden",
            "Guid",
            "OptionText",
            "ChineseOptionText"
        };
    }

}
