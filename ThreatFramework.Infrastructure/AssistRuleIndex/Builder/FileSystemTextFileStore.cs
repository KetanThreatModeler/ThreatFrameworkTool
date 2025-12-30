using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatModeler.TF.Infra.Contract.AssistRuleIndex.Common.Writer;

namespace ThreatModeler.TF.Infra.Implmentation.AssistRuleIndex.Builder
{
    public sealed class FileSystemTextFileStore : ITextFileStore
    {
        public async Task WriteAllTextAsync(string path, string content)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllTextAsync(path, content);
        }

        public Task<string> ReadAllTextAsync(string path)
            => File.ReadAllTextAsync(path);
    }
}