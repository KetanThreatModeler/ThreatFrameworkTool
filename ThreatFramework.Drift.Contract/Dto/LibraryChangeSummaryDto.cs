using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ThreatModeler.TF.Drift.Contract.Dto
{
    public sealed class LibraryChangeSummaryDto
    {
        public string LibraryName { get; init; } = string.Empty;
        public Guid Guid { get; init; }
        public string Operation { get; init; } = string.Empty;
        public string? LocalLibraryVersion { get; init; }
        public string? LibraryVersion { get; init; }
        public string? ReleaseNote { get; init; }

        public string CompanyName = "ThreatModeler Software Inc.";
        public string CompanyEmail { get; init; } = "support@threatmodeler.com";
        public string CompanyURL { get; init; } = "https://www.threatmodeler.com";  
        public string CompanyPhone { get; init; } = "2012660510";
        public string libraryDescription { get; init; } = "v7.4-11142025";
        public string Address { get; init; } = "USA";
        public string LogoURL { get; init; } = "asd";
        public DateTime ReleaseDate { get; init; } = DateTime.UtcNow;
    }
}
