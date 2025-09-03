using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatFramework.YamlFileGenerator.Impl
{
        /// <summary>
        /// Utility class for writing YAML files
        /// </summary>
        internal static class YamlFileWriter
        {
            /// <summary>
            /// Writes content to a YAML file
            /// </summary>
            /// <param name="fileName">The name/path of the file to create</param>
            /// <param name="content">The content to write to the file</param>
            /// <returns>A task representing the asynchronous write operation</returns>
            public static async Task WriteFileAsync(string fileName, string content)
            {
                if (string.IsNullOrWhiteSpace(fileName))
                    throw new ArgumentException("File name cannot be null or empty", nameof(fileName));
            
                if (content == null)
                    throw new ArgumentNullException(nameof(content));

                // Ensure the directory exists
                var directory = Path.GetDirectoryName(fileName);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                await File.WriteAllTextAsync(fileName, content, Encoding.UTF8);
            }

            /// <summary>
            /// Writes content to a YAML file synchronously
            /// </summary>
            /// <param name="fileName">The name/path of the file to create</param>
            /// <param name="content">The content to write to the file</param>
            public static void WriteFile(string fileName, string content)
            {
                if (string.IsNullOrWhiteSpace(fileName))
                    throw new ArgumentException("File name cannot be null or empty", nameof(fileName));
            
                if (content == null)
                    throw new ArgumentNullException(nameof(content));

                // Ensure the directory exists
                var directory = Path.GetDirectoryName(fileName);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(fileName, content, Encoding.UTF8);
            }
        }
}
