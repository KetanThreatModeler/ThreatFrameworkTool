using ThreatFramework.Core.CoreEntities;
using ThreatFramework.Drift.Contract.Model;
using ThreatModeler.TF.Drift.Contract;
using ThreatModeler.TF.Drift.Contract.MappingDriftService.Dto;
using ThreatModeler.TF.Drift.Contract.Model.UpdatedFinal;

namespace ThreatModeler.TF.Drift.Implemenetation
{
    public sealed class TMFrameworkDriftConverter : ITMFrameworkDriftConverter
    {
        private readonly IYamlRouter _yamlRouter;

        public TMFrameworkDriftConverter(IYamlRouter yamlRouter)
        {
            _yamlRouter = yamlRouter ?? throw new ArgumentNullException(nameof(yamlRouter));
        }

        public async Task<TMFrameworkDrift1> ConvertAsync(TMFrameworkDriftDto source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            // Modified libraries require YAML lookups → async
            var modifiedLibsTasks = (source.ModifiedLibraries ?? new List<LibraryDriftDto>())
                .Select(ConvertLibraryDriftAsync);
            var modifiedLibs = await Task.WhenAll(modifiedLibsTasks).ConfigureAwait(false);

            var addedLibsTasks = (source.AddedLibraries ?? new List<AddedLibraryDto>())
                .Select(ConvertAddedLibraryAsync);
            var addedLibs = await Task.WhenAll(addedLibsTasks).ConfigureAwait(false);

            var deletedLibsTasks = (source.DeletedLibraries ?? new List<DeletedLibraryDto>())
                .Select(ConvertDeletedLibraryAsync);
            var deletedLibs = await Task.WhenAll(deletedLibsTasks).ConfigureAwait(false);

            return new TMFrameworkDrift1
            {
                Global = ConvertGlobal(source.Global),
                ModifiedLibraries = modifiedLibs.ToList(),
                AddedLibraries = addedLibs.ToList(),
                DeletedLibraries = deletedLibs.ToList()
            };
        }

        #region Global

        private static GlobalDrift1 ConvertGlobal(GlobalDrift global)
        {
            if (global == null) return null;

            return new GlobalDrift1
            {
                PropertyOptions = global.PropertyOptions,
                PropertyTypes = global.PropertyTypes,
                ComponentTypes = global.ComponentTypes
            };
        }

        #endregion

        #region Libraries

        private async Task<LibraryDrift1> ConvertLibraryDriftAsync(LibraryDriftDto source)
        {
            if (source == null) return null;

            var library = await _yamlRouter
                .GetLibraryByGuidAsync(source.LibraryGuid, DriftSource.Client)
                .ConfigureAwait(false);

            var components = await ConvertComponentDriftAsync(source.Components).ConfigureAwait(false);
            var threats = await ConvertThreatDriftAsync(source.Threats).ConfigureAwait(false);

            return new LibraryDrift1
            {
                library = library,
                LibraryChanges = source.LibraryChanges ?? new(),
                Components = components,
                Threats = threats,
                SecurityRequirements = source.SecurityRequirements,
                TestCases = source.TestCases,
                Properties = source.Properties
            };
        }

        private async Task<AddedLibrary1> ConvertAddedLibraryAsync(AddedLibraryDto source)
        {
            if (source == null) return null;


            return new AddedLibrary1
            {
                // Added library already has full Library object
                Library = source.Library,
                Components = await ConvertAddedComponentsAsync(source.Components),
                Threats = await ToAddedThreat1ListAsync(source.Threats),
                SecurityRequirements = source.SecurityRequirements ?? new(),
                TestCases = source.TestCases ?? new(),
                Properties = source.Properties ?? new()
            };
        }

        private async Task<DeletedLibrary1> ConvertDeletedLibraryAsync(DeletedLibraryDto source)
        {
            if (source == null) return null;

            return new DeletedLibrary1
            {
                LibraryGuid = source.LibraryGuid,
                LibraryName = source.LibraryName,
                Components = await ConvertDeletedComponentsAsync(source.Components),
                Threats = await ToDeletedThreat1ListAsync(source.Threats),
                SecurityRequirements = source.SecurityRequirements ?? new(),
                TestCases = source.TestCases ?? new(),
                Properties = source.Properties ?? new()
            };
        }

        #endregion

        #region Components & mappings

        private async Task<ComponentDrift1> ConvertComponentDriftAsync(ComponentDriftDto source)
        {
            if (source == null) return new ComponentDrift1();

            var added = new List<AddedComponent1>();
            var removed = new List<DeletedComponent1>();
            var modified = new List<ModifiedComponent1>();

            if (source.Added != null)
            {
                foreach (var a in source.Added)
                {
                    added.Add(await ConvertAddedComponentAsync(a).ConfigureAwait(false));
                }
            }

            if (source.Deleted != null)
            {
                foreach (var d in source.Deleted)
                {
                    removed.Add(await ConvertDeletedComponentAsync(d).ConfigureAwait(false));
                }
            }

            if (source.Modified != null)
            {
                foreach (var m in source.Modified)
                {
                    modified.Add(await ConvertModifiedComponentAsync(m).ConfigureAwait(false));
                }
            }

            return new ComponentDrift1
            {
                Added = added,
                Removed = removed,
                Modified = modified
            };
        }

        private async Task<List<AddedComponent1>> ConvertAddedComponentsAsync(IEnumerable<AddedComponentDto> sources)
        {
            if (sources == null) return new List<AddedComponent1>();

            var tasks = sources.Select(ConvertAddedComponentAsync);
            return (await Task.WhenAll(tasks).ConfigureAwait(false)).ToList();
        }

        private async Task<AddedComponent1> ConvertAddedComponentAsync(AddedComponentDto source)
        {
            if (source == null) return null;

            return new AddedComponent1
            {
                Component = source.Component,
                // Added component → resolve mappings from GoldenDb (new/added side)
                Mappings = await ConvertMappingsAsync(source.Mappings, DriftSource.GoldenDb)
                    .ConfigureAwait(false)
            };
        }

        private async Task<List<DeletedComponent1>> ConvertDeletedComponentsAsync(IEnumerable<DeletedComponentDto> sources)
        {
            if (sources == null) return new List<DeletedComponent1>();
            var tasks = sources.Select(ConvertDeletedComponentAsync);
            return (await Task.WhenAll(tasks).ConfigureAwait(false)).ToList();
        }
        private async Task<DeletedComponent1> ConvertDeletedComponentAsync(DeletedComponentDto source)
        {
            if (source == null) return null;

            return new DeletedComponent1
            {
                Component = source.Component,
                // Deleted component → resolve mappings from Client side
                Mappings = await ConvertMappingsAsync(source.Mappings, DriftSource.Client)
                    .ConfigureAwait(false)
            };
        }

        private async Task<ModifiedComponent1> ConvertModifiedComponentAsync(ModifiedComponentDto source)
        {
            if (source == null) return null;
            var componentFromYaml = await _yamlRouter
        .GetComponentByGuidAsync(source.Component.Guid, DriftSource.Client);

            var mappingsAdded = await ConvertMappingsAsync(source.MappingsAdded, DriftSource.GoldenDb)
                .ConfigureAwait(false);

            var mappingsRemoved = await ConvertMappingsAsync(source.MappingsRemoved, DriftSource.Client)
                .ConfigureAwait(false);

            return new ModifiedComponent1
            {
                Component = componentFromYaml,
                ChangedFields = source.ChangedFields ?? new(),
                MappingsAdded = mappingsAdded,
                MappingsRemoved = mappingsRemoved
            };
        }

        private async Task<ComponentMappingCollection1> ConvertMappingsAsync(
            ComponentMappingCollectionDto source,
            DriftSource dataSource)
        {
            if (source == null) return new ComponentMappingCollection1();

            var srs = await ConvertSrMappingsAsync(source.SecurityRequirements, dataSource)
                .ConfigureAwait(false);

            var threatSrMappings = await ConvertThreatSrMappingsAsync(source.ThreatSRMappings, dataSource)
                .ConfigureAwait(false);

            var propertyThreatSrMappings = await ConvertPropertyThreatSrMappingsAsync(source.PropertyThreatSRMappings, dataSource)
                .ConfigureAwait(false);

            return new ComponentMappingCollection1
            {
                SecurityRequirements = srs,
                ThreatSRMappings = threatSrMappings,
                PropertyThreatSRMappings = propertyThreatSrMappings
            };
        }

        #endregion

        #region Mapping conversions

        private async Task<List<SecurityRequirement>> ConvertSrMappingsAsync(
            List<SRMappingDto> source,
            DriftSource dataSource)
        {
            if (source == null) return new();

            var uniqueIds = source
                .Select(x => x.SecurityRequirementId)
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToList();

            var tasks = uniqueIds
                .Select(id => _yamlRouter.GetSecurityRequirementByGuidAsync(id, dataSource));

            var srs = await Task.WhenAll(tasks).ConfigureAwait(false);
            return srs.ToList();
        }

        private async Task<List<ThreatSRMappingDto1>> ConvertThreatSrMappingsAsync(
            List<ThreatSRMappingDto> source,
            DriftSource dataSource)
        {
            if (source == null) return new();

            var result = new List<ThreatSRMappingDto1>();

            var groups = source.GroupBy(m => m.ThreatId);

            foreach (var group in groups)
            {
                var threat = await _yamlRouter
                    .GetThreatByGuidAsync(group.Key, dataSource)
                    .ConfigureAwait(false);

                var srIds = group
                    .Select(m => m.SRId)
                    .Where(id => id.HasValue && id.Value != Guid.Empty)
                    .Select(id => id.Value)
                    .Distinct()
                    .ToList();

                var srTasks = srIds
                    .Select(id => _yamlRouter.GetSecurityRequirementByGuidAsync(id, dataSource));

                var srs = await Task.WhenAll(srTasks).ConfigureAwait(false);

                result.Add(new ThreatSRMappingDto1
                {
                    threat = threat,
                    securityRequirements = srs.ToList()
                });
            }

            return result;
        }

        private async Task<List<PropertyThreatSRMappingDto1>> ConvertPropertyThreatSrMappingsAsync(
            List<PropertyThreatSRMappingDto> source,
            DriftSource dataSource)
        {
            if (source == null || source.Count == 0)
                return new();

            var result = new List<PropertyThreatSRMappingDto1>();

            var propertyGroups = source.GroupBy(m => m.PropertyId);

            foreach (var propertyGroup in propertyGroups)
            {
                var propertyId = propertyGroup.Key;
                if (propertyId == Guid.Empty) continue;

                var property = await _yamlRouter
                    .GetPropertyByGuidAsync(propertyId, dataSource)
                    .ConfigureAwait(false);

                // Group by PropertyOptionId (non-empty)
                var optionGroups = propertyGroup
                    .Where(m => m.PropertyOptionId != Guid.Empty)
                    .GroupBy(m => m.PropertyOptionId);

                var optionDtos = new List<PropertyOptionThreatSRMappingDto1>();

                foreach (var optionGroup in optionGroups)
                {
                    var optionId = optionGroup.Key;
                    var propertyOption = await _yamlRouter
                        .GetPropertyOptionByGuidAsync(optionId, dataSource)
                        .ConfigureAwait(false);

                    // Group by ThreatId (non-empty)
                    var threatGroups = optionGroup
                        .Where(m => m.ThreatId != Guid.Empty)
                        .GroupBy(m => m.ThreatId);

                    var threatDtos = new List<ThreatSRMappingDto1>();

                    foreach (var threatGroup in threatGroups)
                    {
                        var threatId = threatGroup.Key;
                        var threat = await _yamlRouter
                            .GetThreatByGuidAsync(threatId, dataSource)
                            .ConfigureAwait(false);

                        var srIds = threatGroup
                            .Select(m => m.SRId)
                            .Where(id => id != Guid.Empty)
                            .Distinct()
                            .ToList();

                        var srTasks = srIds
                            .Select(id => _yamlRouter.GetSecurityRequirementByGuidAsync(id, dataSource));

                        var srs = await Task.WhenAll(srTasks).ConfigureAwait(false);

                        threatDtos.Add(new ThreatSRMappingDto1
                        {
                            threat = threat,
                            securityRequirements = srs.ToList()
                        });
                    }

                    optionDtos.Add(new PropertyOptionThreatSRMappingDto1
                    {
                        PropertyOption = propertyOption,
                        ThreatSRMappings = threatDtos
                    });
                }

                result.Add(new PropertyThreatSRMappingDto1
                {
                    Property = property,
                    PropertyOptionThreatSRMapping = optionDtos
                });
            }

            return result;
        }


        #endregion


        #region ThreatRegion
        private Task<ThreatMappingCollection1> ToThreatMappingCollection1Async(ThreatMappingCollectionDto source, DriftSource dataSource)
        {
            if (source == null) return Task.FromResult(new ThreatMappingCollection1());
            ThreatMappingCollection1 threatMappingCollection1 = new ThreatMappingCollection1();
            foreach (var srMapping in source.SecurityRequirements)
            {
                var sr = _yamlRouter.GetSecurityRequirementByGuidAsync(srMapping.SecurityRequirementId, dataSource).Result;
                threatMappingCollection1.SecurityRequirement.Add(sr);
            }
            return Task.FromResult(threatMappingCollection1);
        }

        private async Task<List<AddedThreat1>> ToAddedThreat1ListAsync(List<AddedThreatDto> addedThreats)
        {
            var addedThreat1List = new List<AddedThreat1>();
            if (addedThreats == null) return addedThreat1List;
            foreach (var addedThreat in addedThreats)
            {
                var addedThreat1 = await ToAddedThreat1(addedThreat);
                addedThreat1List.Add(addedThreat1);
            }
            return addedThreat1List;
        }

        private async Task<List<DeletedThreat1>> ToDeletedThreat1ListAsync(List<RemovedThreatDto> removedThreats)
        {
            var deletedThreat1List = new List<DeletedThreat1>();
            if (removedThreats == null) return deletedThreat1List;
            foreach (var removedThreat in removedThreats)
            {
                var deletedThreat1 = await ToDeletedThreat1(removedThreat);
                deletedThreat1List.Add(deletedThreat1);
            }
            return deletedThreat1List;
        }

        private async Task<List<ModifiedThreat1>> ToModifiedThreat1ListAsync(List<ModifiedThreatDto> modifiedThreats)
        {
            var modifiedThreat1List = new List<ModifiedThreat1>();
            if (modifiedThreats == null) return modifiedThreat1List;
            foreach (var modifiedThreat in modifiedThreats)
            {
                var modifiedThreat1 = await ToModifiedThreat1(modifiedThreat);
                modifiedThreat1List.Add(modifiedThreat1);
            }
            return modifiedThreat1List;
        }

        private async Task<AddedThreat1> ToAddedThreat1(AddedThreatDto addedThreat)
        {
            if (addedThreat == null) return await Task.FromResult<AddedThreat1>(null);

            var threat = await _yamlRouter.GetThreatByGuidAsync(addedThreat.Threat.Guid, DriftSource.GoldenDb);
            var threatMappingCollection1 = await ToThreatMappingCollection1Async(addedThreat.Mappings, DriftSource.GoldenDb);
            var addedThreat1 = new AddedThreat1
            {
                Threat = threat,
                Added = threatMappingCollection1
            };
            return addedThreat1;
        }

        private async Task<DeletedThreat1> ToDeletedThreat1(RemovedThreatDto removedThreat)
        {
            if (removedThreat == null) return await Task.FromResult<DeletedThreat1>(null);

            var threat = await _yamlRouter.GetThreatByGuidAsync(removedThreat.Threat.Guid, DriftSource.Client);
            var threatMappingCollection1 = await ToThreatMappingCollection1Async(removedThreat.Mappings, DriftSource.Client);
            var deletedThreat1 = new DeletedThreat1
            {
                Threat = threat,
                Removed = threatMappingCollection1
            };
            return deletedThreat1;
        }

        private async Task<ModifiedThreat1> ToModifiedThreat1(ModifiedThreatDto modifiedThreat)
        {
            if (modifiedThreat == null) return await Task.FromResult<ModifiedThreat1>(null);

            var threat = await _yamlRouter.GetThreatByGuidAsync(modifiedThreat.Threat.Guid, DriftSource.Client);
            var mappingsAdded = await ToThreatMappingCollection1Async(modifiedThreat.MappingsAdded, DriftSource.GoldenDb);
            var mappingsRemoved = await ToThreatMappingCollection1Async(modifiedThreat.MappingsRemoved, DriftSource.Client);
            var modifiedThreat1 = new ModifiedThreat1
            {
                Threat = threat,
                ChangedFields = modifiedThreat.ChangedFields,
                Added = mappingsAdded,
                Removed = mappingsRemoved
            };
            return modifiedThreat1;
        }
        
        private async Task<ThreatDrift1> ConvertThreatDriftAsync(ThreatDriftDto source)
        {
            if (source == null) return new ThreatDrift1();
            var added = await ToAddedThreat1ListAsync(source.Added).ConfigureAwait(false);
            var deleted = await ToDeletedThreat1ListAsync(source.Removed).ConfigureAwait(false);
            var modified = await ToModifiedThreat1ListAsync(source.Modified).ConfigureAwait(false);
            return new ThreatDrift1
            {
                Added = added,
                Deleted = deleted,
                Modified = modified
            };
        }
        #endregion
    }
}
