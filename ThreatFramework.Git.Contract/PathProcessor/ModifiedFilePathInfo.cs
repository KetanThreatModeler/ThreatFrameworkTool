using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatModeler.TF.Git.Contract.PathProcessor
{
    public sealed class ModifiedFilePathInfo
    {
        public ModifiedFilePathInfo(
            string relativePath,
            string baseRepositoryFilePath,
            string targetRepositoryFilePath)
        {
            RelativePath = relativePath ?? throw new ArgumentNullException(nameof(relativePath));
            BaseRepositoryFilePath = baseRepositoryFilePath ?? throw new ArgumentNullException(nameof(baseRepositoryFilePath));
            TargetRepositoryFilePath = targetRepositoryFilePath ?? throw new ArgumentNullException(nameof(targetRepositoryFilePath));
        }

        /// <summary>
        /// Relative path within the repository (e.g. "01/components/2.yaml").
        /// </summary>
        public string RelativePath { get; }

        /// <summary>
        /// Absolute file path in the base/source repository.
        /// </summary>
        public string BaseRepositoryFilePath { get; }

        /// <summary>
        /// Absolute file path in the target repository.
        /// </summary>
        public string TargetRepositoryFilePath { get; }
    }
}
