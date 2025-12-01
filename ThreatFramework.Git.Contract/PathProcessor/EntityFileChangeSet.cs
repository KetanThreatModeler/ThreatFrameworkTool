using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatModeler.TF.Git.Contract.PathProcessor
{
    public sealed class EntityFileChangeSet
    {
        public List<string> AddedFilePaths { get; } = new();
        public List<string> DeletedFilePaths { get; } = new();
        public List<ModifiedFilePathInfo> ModifiedFiles { get; } = new();
    }

}
