using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatModeler.TF.Core.Model.Global;

namespace ThreatModeler.TF.Infra.Contract.Repository.Global
{
    public interface IComponentTypeRepository
    {
        Task<IEnumerable<ComponentType>> GetComponentTypesAsync();
        Task<IEnumerable<Guid>> GetGuidsAndLibraryGuidsAsync();
    }
}
