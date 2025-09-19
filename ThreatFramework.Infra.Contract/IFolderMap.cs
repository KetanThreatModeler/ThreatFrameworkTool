using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatFramework.Infra.Contract
{
    public interface IFolderMap
    {
        string GetSubfolderFor<T>(); // e.g., typeof(Threat) => "threats"
        bool TryGetEntityKindFromRelativePath(string relativePath, out string entityKind);
    }
}
