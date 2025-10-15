﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatFramework.Infra.Contract.Index
{
    public interface IGuidIndexRepository
    {
        Task<IReadOnlyDictionary<Guid, int>> LoadAsync(string path, CancellationToken ct = default);
    }
}
