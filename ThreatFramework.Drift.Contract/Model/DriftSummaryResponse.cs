using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatFramework.Drift.Contract.Model
{
    public record DriftSummaryResponse(
       string RemoteRepoUrl,
       string TargetPath,
       int AddedCount,
       int RemovedCount,
       int ModifiedCount,
       int RenamedCount,
       IReadOnlyList<string> AddedFiles,
       IReadOnlyList<string> RemovedFiles,
       IReadOnlyList<string> ModifiedFiles
   );
}
