using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatFramework.Drift.Contract.Model
{
    public class DriftAnalyzeRequest
    {
        [Required]
        public string BaselineFolderPath { get; set; } = default!;
        [Required]
        public string TargetFolderPath { get; set; } = default!;
        public DriftSummaryResponse? DriftSummaryResponse { get; set; } // optional optimization hint

        // Optional filtering knobs (future UI can pass these)
        public List<string>? IncludeEntities { get; set; } // e.g., ["Threat","Component"]
        public List<Guid>? IncludeLibraries { get; set; } // library GUIDs
        public List<string>? IncludeFields { get; set; } // field names to restrict diffs
    }
}
