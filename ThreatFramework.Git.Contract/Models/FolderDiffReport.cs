using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatModeler.TF.Git.Contract.Models
{
    public class FolderDiffReport
    {
        public List<string> AddedPaths { get; set; } = new List<string>();
        public List<string> DeletedPaths { get; set; } = new List<string>();
        public List<string> ModifiedPaths { get; set; } = new List<string>();

        // Helper to combine results from parallel tasks
        public void Merge(FolderDiffReport other)
        {
            lock (this)
            {
                AddedPaths.AddRange(other.AddedPaths);
                DeletedPaths.AddRange(other.DeletedPaths);
                ModifiedPaths.AddRange(other.ModifiedPaths);
            }
        }
    }
}