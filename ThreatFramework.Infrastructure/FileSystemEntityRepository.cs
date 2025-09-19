using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Infra.Contract;

namespace ThreatFramework.Infrastructure
{
    public class FileSystemEntityRepository<T> : IEntityRepository<T>
    {
        private readonly IFileSystem _fs;
        private readonly IYamlSerializer _yaml;
        private readonly IFolderMap _map;

        public FileSystemEntityRepository(IFileSystem fs, IYamlSerializer yaml, IFolderMap map)
        {
            _fs = fs; _yaml = yaml; _map = map;
        }

        public Task<IReadOnlyCollection<T>> LoadAllAsync(string rootFolder, IEnumerable<string>? onlyRelativePaths = null, System.Threading.CancellationToken ct = default)
        {
            if (!_fs.DirectoryExists(rootFolder))
                throw new DirectoryNotFoundException($"Folder not found: {rootFolder}");

            string folder = _map.GetSubfolderFor<T>();
            string path = _fs.Combine(rootFolder, folder);

            var results = new List<T>();
            if (!_fs.DirectoryExists(path))
                return Task.FromResult((IReadOnlyCollection<T>)results);

            IEnumerable<string> files;
            if (onlyRelativePaths is null)
            {
                files = _fs.EnumerateFiles(path, "*.y*ml", SearchOption.AllDirectories);
            }
            else
            {
                // Filter by the provided relative paths (optimization to avoid parsing everything)
                var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var rp in onlyRelativePaths)
                {
                    if (rp.Replace('\\', '/').StartsWith(folder + "/", StringComparison.OrdinalIgnoreCase))
                    {
                        allowed.Add(rp.Replace('\\', '/'));
                    }
                }

                var all = _fs.EnumerateFiles(path, "*.y*ml", SearchOption.AllDirectories);
                var rootNorm = rootFolder.Replace('\\', '/');
                files = new List<string>();
                foreach (var f in all)
                {
                    var rel = _fs.GetRelativePath(rootNorm, f);
                    if (allowed.Contains(rel))
                        ((List<string>)files).Add(f);
                }
            }

            foreach (var file in files)
            {
                var yaml = _fs.ReadAllText(file);
                try
                {
                    var obj = _yaml.Deserialize<T>(yaml);
                    if (obj != null)
                        results.Add(obj);
                }
                catch (Exception ex)
                {
                    throw new InvalidDataException($"Failed to parse YAML as {typeof(T).Name}: {file}\n{ex.Message}");
                }
            }

            return Task.FromResult((IReadOnlyCollection<T>)results);
        }
    }
}
