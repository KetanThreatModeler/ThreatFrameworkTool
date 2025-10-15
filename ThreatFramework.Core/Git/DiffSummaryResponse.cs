using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatFramework.Core.Git
{
    public record RenamedFile(string From, string To);

    public record DiffSummaryResponse(
        string RemoteRepoUrl,
        string TargetPath,
        int AddedCount,
        int RemovedCount,
        int ModifiedCount,
        int RenamedCount,
        List<string> AddedFiles,
        List<string> RemovedFiles,
        List<string> ModifiedFiles,
        List<RenamedFile> RenamedFiles
    );
}
