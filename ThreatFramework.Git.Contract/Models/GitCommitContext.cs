using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatModeler.TF.Git.Contract.Models
{
    public class GitCommitContext : GitSettings
    {
        // This property is now isolated to this class
        public string CommitMessage { get; set; } = string.Empty;
    }
}
