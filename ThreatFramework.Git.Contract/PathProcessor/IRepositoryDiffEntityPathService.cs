using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatModeler.TF.Git.Contract.Models;

namespace ThreatModeler.TF.Git.Contract.PathProcessor
{
    public interface IRepositoryDiffEntityPathService
    {
        IRepositoryDiffEntityPathContext Create(FolderDiffReport diff);
    }
}