using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatModeler.TF.Infra.Contract.Repository;

namespace ThreatFramework.Infra.Contract.Repository
{
    public interface IRepositoryHubFactory
    {
        IRepositoryHub Create(DataPlane plane);
    }
}
