using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatModeler.TF.Infra.Contract.AssistRuleIndex.Common.Writer
{
    public interface ITextFileStore
    {
        Task WriteAllTextAsync(string path, string content);
        Task<string> ReadAllTextAsync(string path);
    }
}
