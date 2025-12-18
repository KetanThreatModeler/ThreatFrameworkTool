using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatModeler.TF.Core.Model.AssistRules;

namespace ThreatModeler.TF.Infra.Contract.YamlRepository.AssistRules
{
    public interface IYamlRelationshipReader
    {
        Task<Relationship> ReadRelationshipAsync(string filePath);

        Task<IReadOnlyList<Relationship>> ReadRelationshipsAsync(IEnumerable<string> filePaths);
    }
}
