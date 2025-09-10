using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatFramework.Core.Git
{
    public record FolderToFolderDiffRequest
    {
        [Required]
        public string BaselineFolderPath { get; init; } = string.Empty;

        [Required]
        public string TargetFolderPath { get; init; } = string.Empty;
    }
}
