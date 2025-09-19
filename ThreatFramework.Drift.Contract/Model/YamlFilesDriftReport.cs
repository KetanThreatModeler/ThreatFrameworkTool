using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatFramework.Drift.Contract.Model
{
    public class YamlFilesDriftReport
    {
        //consider this folder path golden database path
        public string BaseLineFolderPath { get; set; }

        //consider this folder path as Client ThreatFramework path
        public string TargetFolderPath { get; set; }
        public List<string> AddedFiles { get; set; } = new();
        public List<string> RemovedFiles { get; set; } = new();
        public List<string> ModifiedFiles { get; set; } = new();
    }
}
