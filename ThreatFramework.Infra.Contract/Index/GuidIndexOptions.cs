using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatFramework.Infra.Contract.Index
{
    public sealed class GuidIndexOptions
    {
        public const string SectionName = "GuidIndex";

        [Required, MinLength(10)]
        public string? FilePath { get; set; }
    }
}
