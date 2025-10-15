using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatFramework.Drift.Impl.MappingDriftService.Helper
{
    public interface ISetDiffer<T>
    {
        (List<T> Added, List<T> Removed) Diff(HashSet<T> a, HashSet<T> b);
    }
}
