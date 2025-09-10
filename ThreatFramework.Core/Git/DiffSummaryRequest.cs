using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatFramework.Core.Git
{
    public record DiffSummaryRequest
    {
        [Required]
        public string RemoteRepoUrl { get; init; } = default!;  // remote baseline

        [Required]
        public string TargetPath { get; init; } = default!;     // local folder
    }

}
