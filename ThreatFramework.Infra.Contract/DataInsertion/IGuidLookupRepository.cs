using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Infra.Contract.DataInsertion.Dto;

namespace ThreatFramework.Infra.Contract.DataInsertion
{
    public interface IGuidLookupRepository
    {
        Task<MissingGuidsByEntity> GetMissingGuidsAsync(CheckMissingGuidsRequest request);
    }
}
