using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Infra.Contract;

namespace ThreatFramework.Infrastructure
{
    public class LocalFileSystem : IFileSystem
    {
        public bool DirectoryExists(string path) => Directory.Exists(path);
        public IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption option)
            => Directory.EnumerateFiles(path, searchPattern, option);
        public string Combine(params string[] parts) => Path.Combine(parts);
        public string GetRelativePath(string relativeTo, string path) => Path.GetRelativePath(relativeTo, path).Replace('\\', '/');
        public string ReadAllText(string path) => File.ReadAllText(path);
    }

}
