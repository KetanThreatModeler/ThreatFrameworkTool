using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatFramework.Infra.Contract
{
    public interface IFileSystem
    {
        bool DirectoryExists(string path);
        IEnumerable<string> EnumerateFiles(string path, string searchPattern, System.IO.SearchOption option);
        string Combine(params string[] parts);
        string GetRelativePath(string relativeTo, string path);
        string ReadAllText(string path);
    }
}
