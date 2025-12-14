using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Drift.Contract.Model;

namespace ThreatFramework.Infra.Contract.DataInsertion
{
    public interface IDriftApplier
    {
       Task ApplyAsync(TMFrameworkDriftDto drift);
    }
}
