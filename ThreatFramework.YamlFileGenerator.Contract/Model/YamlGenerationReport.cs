using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatFramework.YamlFileGenerator.Contract.Model
{
    public sealed class YamlGenerationReport
    {
        public string RootPath { get; }
        public int TotalFiles { get; }
        public IReadOnlyDictionary<string, int> ByBucketCounts { get; }

        public YamlGenerationReport(string rootPath, int totalFiles, IDictionary<string, int> byBucketCounts)
        {
            RootPath = rootPath ?? throw new ArgumentNullException(nameof(rootPath));
            TotalFiles = totalFiles;
            ByBucketCounts = new Dictionary<string, int>(byBucketCounts ?? new Dictionary<string, int>(), StringComparer.OrdinalIgnoreCase);
        }
    }
}
