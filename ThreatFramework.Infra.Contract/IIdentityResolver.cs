using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatFramework.Infra.Contract
{
    public interface IIdentityResolver
    {
        string GetEntityKey<T>(T entity); // typically Guid.ToString()
        Guid? GetLibraryGuid<T>(T entity); // resolve library scope: LibraryGuid or LibraryId
    }
}
