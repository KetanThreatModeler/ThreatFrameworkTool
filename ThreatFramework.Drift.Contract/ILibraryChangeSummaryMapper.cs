using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatModeler.TF.Drift.Contract.Dto;
using ThreatModeler.TF.Drift.Contract.Model;

namespace ThreatModeler.TF.Drift.Contract
{
    public interface ILibraryChangeSummaryMapper
    {
        IReadOnlyList<LibraryChangeSummaryDto> Map(TMFrameworkDrift drift);
    }
}
