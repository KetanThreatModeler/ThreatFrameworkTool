using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatModeler.TF.Git.Contract.Models;
using ThreatModeler.TF.Git.Contract.PathProcessor;

namespace ThreatModeler.TF.Git.Implementation.PathProcessor
{
    public sealed class RepositoryDiffEntityPathService : IRepositoryDiffEntityPathService
    {
        private readonly IPathClassifier _pathClassifier;

        public RepositoryDiffEntityPathService(IPathClassifier pathClassifier)
        {
            _pathClassifier = pathClassifier ?? throw new ArgumentNullException(nameof(pathClassifier));
        }

        public IRepositoryDiffEntityPathContext Create(FolderDiffReport diff)
        {
            if (diff == null) throw new ArgumentNullException(nameof(diff));
            return new RepositoryDiffEntityPathContext(_pathClassifier, diff);
        }
    }
}