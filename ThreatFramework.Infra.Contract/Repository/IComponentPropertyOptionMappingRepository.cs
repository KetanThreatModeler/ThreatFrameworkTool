﻿using ThreatFramework.Core.Models.PropertyMapping;

namespace ThreatFramework.Infra.Contract.Repository
{
    public interface IComponentPropertyOptionMappingRepository
    {

        Task<IEnumerable<ComponentPropertyOptionMapping>> GetMappingsByLibraryGuidAsync(IEnumerable<Guid> libraryGuidsds);
        Task<IEnumerable<ComponentPropertyOptionMapping>> GetReadOnlyMappingsAsync();
    }
}
