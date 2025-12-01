using System;
using System.Collections.Generic;
using ThreatModeler.TF.Git.Contract.PathProcessor;

namespace ThreatModeler.TF.Git.Contract.Models
{
    /// <summary>
    /// Represents a diff report between two repositories.
    /// Paths are relative, but the base/target repository
    /// paths are available to construct absolute paths.
    /// </summary>
    public sealed class FolderDiffReport
    {
        public FolderDiffReport()
        {
        }

        public FolderDiffReport(string baseRepositoryPath, string targetRepositoryPath)
        {
            BaseRepositoryPath = baseRepositoryPath;
            TargetRepositoryPath = targetRepositoryPath;
        }

        /// <summary>
        /// Base / source repository path used in the comparison.
        /// </summary>
        public string? BaseRepositoryPath { get; private set; }

        /// <summary>
        /// Target repository path used in the comparison.
        /// </summary>
        public string? TargetRepositoryPath { get; private set; }

        /// <summary>
        /// Relative paths that exist only in the base repository (added in base vs target).
        /// </summary>
        public List<string> AddedPaths { get; } = new();

        /// <summary>
        /// Relative paths that exist only in the target repository (deleted in base vs target).
        /// </summary>
        public List<string> DeletedPaths { get; } = new();

        /// <summary>
        /// Relative paths that exist in both repositories but differ in content or type.
        /// </summary>
        public List<string> ModifiedPaths { get; } = new();

        /// <summary>
        /// Merges another report into this one. Intended for combining
        /// results from parallel comparisons for the same repositories.
        /// </summary>
        public void Merge(FolderDiffReport other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));

            lock (this)
            {
                // Preserve repository paths if they are not already set.
                if (string.IsNullOrWhiteSpace(BaseRepositoryPath) &&
                    !string.IsNullOrWhiteSpace(other.BaseRepositoryPath))
                {
                    BaseRepositoryPath = other.BaseRepositoryPath;
                }

                if (string.IsNullOrWhiteSpace(TargetRepositoryPath) &&
                    !string.IsNullOrWhiteSpace(other.TargetRepositoryPath))
                {
                    TargetRepositoryPath = other.TargetRepositoryPath;
                }

                AddedPaths.AddRange(other.AddedPaths);
                DeletedPaths.AddRange(other.DeletedPaths);
                ModifiedPaths.AddRange(other.ModifiedPaths);
            }
        }
    }
}
