using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatFramework.Core
{
    public sealed class EntityDriftAggregationOptions
    {
        public IReadOnlyList<string> LibraryDefaultFields { get; init; } = new[]
        {
            "Readonly",
            "IsDefault",
            "sharingtype",
            "version",
            "ReleaseNotes",
            "ImageUrl",
            "Name",
            "Version",
            "Description"
        };

        public IReadOnlyList<string> ThreatDefaultFields { get; init; } = new[]
        {
            "RiskId",
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

        public IReadOnlyList<string> ComponentDefaultFields { get; init; } = new[]
        {
            "LibraryGuid",
            "ComponentTypeId",
            "IsHidden",
            "Name",
            "ImagePath",
            "Labels",
            "Version",
            "Description",
            "ChineseDescription"
        };
        public IReadOnlyList<string> SecurityRequirementDefaultFields { get; init; } = new[] {
            "RiskId",
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

        public IReadOnlyList<string> TestCaseDefaultFields { get; init; } = new[] {
            "LibraryId",
            "IsHidden",
            "CreatedDate",
            "LastUpdated",
            "Guid",
            "Name",
            "ChineseName",
            "Labels",
            "Description",
            "ChineseDescription"
        };


        public IReadOnlyList<string> PropertyDefaultFields { get; init; } = new[] {
        "LibraryId",
        "PropertyTypeId",
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
    }

}
