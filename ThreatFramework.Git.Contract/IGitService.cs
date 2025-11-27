using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatModeler.TF.Git.Contract.Models;

namespace ThreatModeler.TF.Git.Contract
{
    public interface IGitService
    {
        void SyncRepository(GitSettings settings);
        void CommitAndPush(GitCommitContext context);
    }
}
