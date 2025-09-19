using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Drift.Contract.Model;

namespace ThreatFramework.Drift.Contract
{
    public interface IDiffEngine<T>
    {
        EntityDiff<T> Diff(
            IReadOnlyCollection<T> baseline,
            IReadOnlyCollection<T> target,
            Func<string, bool>? includeFieldPredicate = null);
    }
}
