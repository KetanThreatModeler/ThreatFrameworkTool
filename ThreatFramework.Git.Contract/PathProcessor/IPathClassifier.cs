using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatModeler.TF.Git.Contract.PathProcessor
{
    public interface IPathClassifier
    {
        DomainPathInfo Classify(string relativePath);
    }
}
