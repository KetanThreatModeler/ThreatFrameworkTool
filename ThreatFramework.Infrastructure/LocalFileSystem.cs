using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using ThreatFramework.Infra.Contract;

namespace ThreatFramework.Infrastructure
{
    public class LocalFileSystem : IFileSystem
    {
        private readonly ILogger<LocalFileSystem> _logger;

        public LocalFileSystem(ILogger<LocalFileSystem> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool DirectoryExists(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                _logger.LogWarning("DirectoryExists called with null/empty path.");
                return false;
            }

            try
            {
                var exists = Directory.Exists(path);
                _logger.LogDebug("DirectoryExists('{Path}') => {Exists}", path, exists);
                return exists;
            }
            catch (Exception ex) when (ex is ArgumentException ||
                                       ex is PathTooLongException ||
                                       ex is NotSupportedException ||
                                       ex is UnauthorizedAccessException ||
                                       ex is IOException)
            {
                _logger.LogError(ex, "Error checking directory existence for path '{Path}'.", path);
                return false;
            }
        }

        public IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption option)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path cannot be null or empty.", nameof(path));

            if (string.IsNullOrWhiteSpace(searchPattern))
                throw new ArgumentException("Search pattern cannot be null or empty.", nameof(searchPattern));

            try
            {
                _logger.LogDebug("EnumerateFiles('{Path}', '{SearchPattern}', {Option})", path, searchPattern, option);
                return Directory.EnumerateFiles(path, searchPattern, option);
            }
            catch (Exception ex) when (ex is ArgumentException ||
                                       ex is DirectoryNotFoundException ||
                                       ex is PathTooLongException ||
                                       ex is NotSupportedException ||
                                       ex is UnauthorizedAccessException ||
                                       ex is IOException)
            {
                _logger.LogError(ex,
                    "Error enumerating files. Path='{Path}', SearchPattern='{SearchPattern}', Option={Option}",
                    path, searchPattern, option);
                throw;
            }
        }

        public string Combine(params string[] parts)
        {
            if (parts is null)
                throw new ArgumentNullException(nameof(parts));

            try
            {
                var combined = Path.Combine(parts);
                _logger.LogDebug("Combine({PartsCount} parts) => '{Combined}'", parts.Length, combined);
                return combined;
            }
            catch (Exception ex) when (ex is ArgumentException ||
                                       ex is PathTooLongException ||
                                       ex is NotSupportedException)
            {
                _logger.LogError(ex, "Error combining path parts.");
                throw;
            }
        }

        public string GetRelativePath(string relativeTo, string path)
        {
            if (string.IsNullOrWhiteSpace(relativeTo))
                throw new ArgumentException("relativeTo cannot be null or empty.", nameof(relativeTo));
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("path cannot be null or empty.", nameof(path));

            try
            {
                var rel = Path.GetRelativePath(relativeTo, path).Replace('\\', '/');
                _logger.LogDebug("GetRelativePath(RelativeTo='{RelativeTo}', Path='{Path}') => '{Relative}'",
                    relativeTo, path, rel);
                return rel;
            }
            catch (Exception ex) when (ex is ArgumentException ||
                                       ex is PathTooLongException ||
                                       ex is NotSupportedException ||
                                       ex is UnauthorizedAccessException ||
                                       ex is IOException)
            {
                _logger.LogError(ex,
                    "Error getting relative path. RelativeTo='{RelativeTo}', Path='{Path}'",
                    relativeTo, path);
                throw;
            }
        }

        public string ReadAllText(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path cannot be null or empty.", nameof(path));

            try
            {
                _logger.LogDebug("ReadAllText('{Path}')", path);
                return File.ReadAllText(path);
            }
            catch (Exception ex) when (ex is ArgumentException ||
                                       ex is FileNotFoundException ||
                                       ex is DirectoryNotFoundException ||
                                       ex is PathTooLongException ||
                                       ex is NotSupportedException ||
                                       ex is UnauthorizedAccessException ||
                                       ex is IOException)
            {
                _logger.LogError(ex, "Error reading file '{Path}'.", path);
                throw;
            }
        }

        public bool FileExists(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                _logger.LogWarning("FileExists called with null/empty path.");
                return false;
            }

            try
            {
                var exists = File.Exists(path);
                _logger.LogDebug("FileExists('{Path}') => {Exists}", path, exists);
                return exists;
            }
            catch (Exception ex) when (ex is ArgumentException ||
                                       ex is PathTooLongException ||
                                       ex is NotSupportedException ||
                                       ex is UnauthorizedAccessException ||
                                       ex is IOException)
            {
                _logger.LogError(ex, "Error checking file existence for path '{Path}'.", path);
                return false;
            }
        }

        public async Task AtomicWriteAllTextAsync(string path, string content)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path cannot be null or empty.", nameof(path));

            try
            {
                var fullPath = Path.GetFullPath(path);
                var dir = Path.GetDirectoryName(fullPath);

                if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                // same dir so Replace is atomic on same volume
                var tempPath = Path.Combine(
                    dir ?? ".",
                    $"{Path.GetFileName(fullPath)}.tmp_{Guid.NewGuid():N}");

                _logger.LogDebug("Atomic write start. Target='{Target}', Temp='{Temp}'", fullPath, tempPath);

                try
                {
                    await File.WriteAllTextAsync(tempPath, content).ConfigureAwait(false);

                    if (File.Exists(fullPath))
                    {
                        // atomic replace on Windows; on Linux it behaves as replace as well
                        File.Replace(tempPath, fullPath, null);
                    }
                    else
                    {
                        File.Move(tempPath, fullPath);
                    }

                    _logger.LogInformation("Atomic write successful. Target='{Target}'", fullPath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Atomic write failed. Target='{Target}', Temp='{Temp}'", fullPath, tempPath);

                    // best-effort cleanup
                    try
                    {
                        if (File.Exists(tempPath))
                            File.Delete(tempPath);
                    }
                    catch (Exception cleanupEx)
                    {
                        _logger.LogWarning(cleanupEx, "Failed to cleanup temp file '{Temp}'", tempPath);
                    }

                    throw;
                }
            }
            catch (Exception ex) when (ex is ArgumentException ||
                                       ex is NotSupportedException ||
                                       ex is PathTooLongException ||
                                       ex is UnauthorizedAccessException ||
                                       ex is IOException)
            {
                _logger.LogError(ex, "AtomicWriteAllTextAsync failed for path '{Path}'", path);
                throw;
            }
        }
    }
}
