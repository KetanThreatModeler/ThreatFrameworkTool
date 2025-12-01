using Microsoft.Extensions.Options;
using System.Runtime.CompilerServices;
using ThreatFramework.Core;
using ThreatFramework.Core.CoreEntities;
using ThreatFramework.Drift.Contract;
using ThreatFramework.Drift.Contract.CoreEntityDriftService;
using ThreatFramework.Drift.Contract.CoreEntityDriftService.Model;
using ThreatFramework.Drift.Contract.FolderDiff;
using ThreatFramework.Drift.Contract.MappingDriftService;
using ThreatFramework.Drift.Contract.MappingDriftService.Dto;
using ThreatFramework.Drift.Contract.Model;
using ThreatFramework.Infra.Contract.YamlRepository.CoreEntity;
using ThreatModeler.TF.Core.Config;
using ThreatModeler.TF.Core.Global;
using ThreatModeler.TF.Git.Contract;

namespace ThreatFramework.Drift.Impl
{
    public class LibraryDriftAggregator : ILibraryDriftAggregator
    {
        private readonly IFolderDiffService _folderDiffService;
        private readonly ICoreEntityDrift _coreEntityDrift;
        private readonly IComponentMappingDriftService _componentMappingDriftService;
        private readonly PathOptions _exportOptions;
        private readonly IYamlComponentReader _yamlComponentReader;
        private readonly IGitFolderDiffService _gitFolderDiffService;

        public LibraryDriftAggregator(IFolderDiffService folderDiffService, ICoreEntityDrift coreEntityDrift,
            IComponentMappingDriftService componentMappingDriftService,
            IOptions<PathOptions> exportOptions, IYamlComponentReader yamlComponentReader,
            IGitFolderDiffService gitFolderDiffService)
        {
            _folderDiffService = folderDiffService;
            _coreEntityDrift = coreEntityDrift ?? throw new ArgumentNullException(nameof(coreEntityDrift));
            _componentMappingDriftService = componentMappingDriftService ?? throw new ArgumentNullException(nameof(componentMappingDriftService));
            _exportOptions = exportOptions?.Value ?? throw new ArgumentNullException(nameof(exportOptions));
            _yamlComponentReader = yamlComponentReader ?? throw new ArgumentNullException(nameof(yamlComponentReader));
            _gitFolderDiffService = gitFolderDiffService ?? throw new ArgumentNullException(nameof(gitFolderDiffService));
        }

        public async Task<TMFrameworkDrift> Drift()
        {
            FolderComparisionResult filesComparionResult = await _folderDiffService.Compare(_exportOptions.TrcOutput, _exportOptions.ClientOutput);
            CoreEntitiesDrift coreEntitiesDrift = await _coreEntityDrift.BuildAsync(filesComparionResult);
            IEnumerable<ComponentMappingDriftDto> componentMappingResult = await _componentMappingDriftService.GetMappingDrift();
            return await Aggregate(coreEntitiesDrift, componentMappingResult);
        }

        public async Task<TMFrameworkDrift> Aggregate(CoreEntitiesDrift report, IEnumerable<ComponentMappingDriftDto> mappingDrift)
        {
            if (report is null)
            {
                throw new ArgumentNullException(nameof(report));
            }

            if (mappingDrift is null)
            {
                throw new ArgumentNullException(nameof(mappingDrift));
            }

            EntityDriftAggregationOptions cfg = new();
            List<ComponentMappingDriftDto> mappingDriftList = mappingDrift.ToList();

            // Step 1: Create component collections with mappings
            ComponentCollections componentCollections = await CreateComponentCollections(report, mappingDriftList);

            // Step 2: Initialize response with library-level changes
            TMFrameworkDrift response = InitializeLibraryResponse(report, cfg);

            // Step 3: Associate components with libraries
            AssociateComponentsWithLibraries(componentCollections, response);

            // Step 4: Process other entities
            ProcessThreats(report, response, cfg);
            ProcessSecurityRequirements(report, response, cfg);
            ProcessTestCases(report, response, cfg);
            ProcessProperties(report, response, cfg);

            // Step 5: Handle global drift
            response.Global = new GlobalDrift
            {
                PropertyOptions = new EntityDiff<PropertyOption>
                {
                    Added = report.PropertyOptions.Added.ToList(),
                    Removed = report.PropertyOptions.Removed.ToList(),
                    Modified = report.PropertyOptions.Modified
                        .Select(pair =>
                        {
                            List<FieldChange> changes = Compare(pair.Existing, pair.Updated, cfg.PropertyOptionDefaultFields);
                            return changes.Count > 0 ? new ModifiedEntity<PropertyOption>
                            {
                                EntityKey = EntityKey.Of(pair.Updated),
                                ModifiedFields = changes
                            } : null;
                        })
                        .Where(modified => modified != null)
                        .ToList()!
                }
            };

            return response;
        }

        private async Task<ComponentCollections> CreateComponentCollections(CoreEntitiesDrift report, List<ComponentMappingDriftDto> mappingDrift)
        {
            ComponentCollections collections = new();
            Dictionary<Guid, ComponentMappingDriftDto> mappingLookup = mappingDrift.ToDictionary(m => m.ComponentGuid);
            HashSet<Guid> processedComponents = new();

            // Process added components
            foreach (Component component in report.Components.Added)
            {
                AddedComponent addedComponent = new() { Component = component };
                if (mappingLookup.TryGetValue(component.Guid, out ComponentMappingDriftDto? mapping))
                {
                    addedComponent.Mappings = new ComponentMappingCollection
                    {
                        SecurityRequirements = mapping.SecurityRequirementsAdded,
                        ThreatSRMappings = mapping.MappingsAdded,
                        PropertyThreatSRMappings = mapping.PropertyThreatSRMappingsAdded
                    };
                }
                collections.Added[component.Guid] = addedComponent;
                _ = processedComponents.Add(component.Guid);
            }

            // Process deleted components
            foreach (Component component in report.Components.Removed)
            {
                DeletedComponent deletedComponent = new() { Component = component };
                if (mappingLookup.TryGetValue(component.Guid, out ComponentMappingDriftDto? mapping))
                {
                    deletedComponent.Mappings = new ComponentMappingCollection
                    {
                        SecurityRequirements = mapping.SecurityRequirementsRemoved,
                        ThreatSRMappings = mapping.MappingsRemoved,
                        PropertyThreatSRMappings = mapping.PropertyThreatSRMappingsRemoved
                    };
                }
                collections.Deleted[component.Guid] = deletedComponent;
                _ = processedComponents.Add(component.Guid);
            }

            // Process modified components
            foreach (EntityPair<Component> pair in report.Components.Modified)
            {
                Component component = pair.Updated;
                List<FieldChange> changes = Compare(pair.Existing, pair.Updated, new EntityDriftAggregationOptions().ComponentDefaultFields);

                ModifiedComponent modifiedComponent = new()
                {
                    Component = component,
                    ChangedFields = changes
                };

                if (mappingLookup.TryGetValue(component.Guid, out ComponentMappingDriftDto? mapping))
                {
                    modifiedComponent.MappingsAdded = new ComponentMappingCollection
                    {
                        SecurityRequirements = mapping.SecurityRequirementsAdded,
                        ThreatSRMappings = mapping.MappingsAdded,
                        PropertyThreatSRMappings = mapping.PropertyThreatSRMappingsAdded
                    };
                    modifiedComponent.MappingsRemoved = new ComponentMappingCollection
                    {
                        SecurityRequirements = mapping.SecurityRequirementsRemoved,
                        ThreatSRMappings = mapping.MappingsRemoved,
                        PropertyThreatSRMappings = mapping.PropertyThreatSRMappingsRemoved
                    };
                }
                collections.Modified[component.Guid] = modifiedComponent;
                _ = processedComponents.Add(component.Guid);
            }

            // Process remaining mapping changes for components not already processed
            foreach (ComponentMappingDriftDto mapping in mappingDrift)
            {
                if (processedComponents.Contains(mapping.ComponentGuid))
                {
                    continue;
                }

                // Check if this mapping has any actual changes
                bool hasChanges = mapping.SecurityRequirementsAdded.Any() ||
                                 mapping.SecurityRequirementsRemoved.Any() ||
                                 mapping.MappingsAdded.Any() ||
                                 mapping.MappingsRemoved.Any() ||
                                 mapping.PropertyThreatSRMappingsAdded.Any() ||
                                 mapping.PropertyThreatSRMappingsRemoved.Any();

                if (!hasChanges)
                {
                    continue;
                }

                // Create a modified component with only mapping changes (no field changes)
                // Note: We need to get the component instance from somewhere - this might need adjustment
                // based on how the component can be retrieved (e.g., from a service or cache)
                ModifiedComponent modifiedComponent = new()
                {
                    Component = await _yamlComponentReader.GetComponentByGuid(mapping.ComponentGuid),
                    ChangedFields = [], // No field changes, only mapping changes
                    MappingsAdded = new ComponentMappingCollection
                    {
                        SecurityRequirements = mapping.SecurityRequirementsAdded,
                        ThreatSRMappings = mapping.MappingsAdded,
                        PropertyThreatSRMappings = mapping.PropertyThreatSRMappingsAdded
                    },
                    MappingsRemoved = new ComponentMappingCollection
                    {
                        SecurityRequirements = mapping.SecurityRequirementsRemoved,
                        ThreatSRMappings = mapping.MappingsRemoved,
                        PropertyThreatSRMappings = mapping.PropertyThreatSRMappingsRemoved
                    }
                };

                collections.Modified[mapping.ComponentGuid] = modifiedComponent;
            }

            return collections;
        }

        private TMFrameworkDrift InitializeLibraryResponse(CoreEntitiesDrift report, EntityDriftAggregationOptions cfg)
        {
            TMFrameworkDrift response = new();

            // Process added libraries
            foreach (Library library in report.Libraries.Added)
            {
                response.AddedLibraries.Add(new AddedLibrary { Library = library });
            }

            // Process deleted libraries
            foreach (Library library in report.Libraries.Removed)
            {
                response.DeletedLibraries.Add(new DeletedLibrary
                {
                    LibraryGuid = library.Guid,
                    LibraryName = library.Name
                });
            }

            // Process modified libraries
            foreach (EntityPair<Library> pair in report.Libraries.Modified)
            {
                List<FieldChange> changes = Compare(pair.Existing, pair.Updated, cfg.LibraryDefaultFields);
                response.ModifiedLibraries.Add(new LibraryDrift
                {
                    LibraryGuid = pair.Updated.Guid,
                    LibraryName = pair.Updated.Name,
                    LibraryChanges = changes
                });
            }

            return response;
        }

        private void AssociateComponentsWithLibraries(ComponentCollections collections, TMFrameworkDrift response)
        {
            // Associate added components
            foreach (AddedComponent component in collections.Added.Values)
            {
                LibraryKey libraryKey = LibraryKey.From(component.Component);
                (AddedLibrary? addedLibrary, _, LibraryDrift? modifiedLibrary) = FindLibraryInResponse(response, libraryKey);
                if (addedLibrary != null)
                {
                    addedLibrary.Components.Add(component);
                }
                else if (modifiedLibrary != null)
                {
                    modifiedLibrary.Components.Added.Add(component);
                }
                else
                {
                    LibraryDrift newLibrary = CreateModifiedLibrary(libraryKey);
                    newLibrary.Components.Added.Add(component);
                    response.ModifiedLibraries.Add(newLibrary);
                }
            }

            // Associate deleted components
            foreach (DeletedComponent component in collections.Deleted.Values)
            {
                LibraryKey libraryKey = LibraryKey.From(component.Component);
                (_, DeletedLibrary? deletedLibrary, LibraryDrift? modifiedLibrary) = FindLibraryInResponse(response, libraryKey);
                if (deletedLibrary != null)
                {
                    deletedLibrary.Components.Add(component);
                }
                else if (modifiedLibrary != null)
                {
                    modifiedLibrary.Components.Deleted.Add(component);
                }
                else
                {
                    LibraryDrift newLibrary = CreateModifiedLibrary(libraryKey);
                    newLibrary.Components.Deleted.Add(component);
                    response.ModifiedLibraries.Add(newLibrary);
                }
            }

            // Associate modified components
            foreach (ModifiedComponent component in collections.Modified.Values)
            {
                LibraryKey libraryKey = LibraryKey.From(component.Component);
                (_, _, LibraryDrift? modifiedLibrary) = FindLibraryInResponse(response, libraryKey);
                if (modifiedLibrary != null)
                {
                    modifiedLibrary.Components.Modified.Add(component);
                }
                else
                {
                    LibraryDrift newLibrary = CreateModifiedLibrary(libraryKey);
                    newLibrary.Components.Modified.Add(component);
                    response.ModifiedLibraries.Add(newLibrary);
                }
            }
        }

        private void ProcessThreats(CoreEntitiesDrift report, TMFrameworkDrift response, EntityDriftAggregationOptions cfg)
        {
            // Process added threats
            foreach (Threat threat in report.Threats.Added)
            {
                LibraryKey libraryKey = LibraryKey.From(threat);
                (AddedLibrary? addedLibrary, DeletedLibrary? deletedLibrary, LibraryDrift? modifiedLibrary) = FindLibraryInResponse(response, libraryKey);

                if (addedLibrary != null)
                {
                    // Library is in added list - add to library's added threats
                    addedLibrary.Threats.Add(threat);
                }
                else if (deletedLibrary != null)
                {
                    // Library is in deleted list - add to library's deleted threats
                    deletedLibrary.Threats.Add(threat);
                }
                else if (modifiedLibrary != null)
                {
                    // Library is in modified list - add to library's added threats
                    modifiedLibrary.Threats.Added.Add(threat);
                }
                else
                {
                    // Library not found - create new modified library and add threat to added
                    LibraryDrift newLibrary = CreateModifiedLibrary(libraryKey);
                    newLibrary.Threats.Added.Add(threat);
                    response.ModifiedLibraries.Add(newLibrary);
                }
            }

            // Process removed threats
            foreach (Threat threat in report.Threats.Removed)
            {
                LibraryKey libraryKey = LibraryKey.From(threat);
                (AddedLibrary? addedLibrary, DeletedLibrary? deletedLibrary, LibraryDrift? modifiedLibrary) = FindLibraryInResponse(response, libraryKey);

                if (addedLibrary != null)
                {
                    // Library is in added list - add to library's added threats (removed from baseline but library is new)
                    addedLibrary.Threats.Add(threat);
                }
                else if (deletedLibrary != null)
                {
                    // Library is in deleted list - add to library's deleted threats
                    deletedLibrary.Threats.Add(threat);
                }
                else if (modifiedLibrary != null)
                {
                    // Library is in modified list - add to library's removed threats
                    modifiedLibrary.Threats.Removed.Add(threat);
                }
                else
                {
                    // Library not found - create new modified library and add threat to removed
                    LibraryDrift newLibrary = CreateModifiedLibrary(libraryKey);
                    newLibrary.Threats.Removed.Add(threat);
                    response.ModifiedLibraries.Add(newLibrary);
                }
            }

            // Process modified threats
            foreach (EntityPair<Threat> pair in report.Threats.Modified)
            {
                Threat threat = pair.Updated;
                LibraryKey libraryKey = LibraryKey.From(threat);
                List<FieldChange> changes = Compare(pair.Existing, pair.Updated, cfg.ThreatDefaultFields);

                if (changes.Count == 0)
                {
                    continue;
                }

                (AddedLibrary? addedLibrary, DeletedLibrary? deletedLibrary, LibraryDrift? modifiedLibrary) = FindLibraryInResponse(response, libraryKey);
                ModifiedEntity<Threat> modifiedEntity = new()
                {
                    EntityKey = EntityKey.Of(threat),
                    ModifiedFields = changes
                };

                if (addedLibrary != null)
                {
                    // Library is in added list - threat modifications are part of the new library
                    // Note: This case might be rare as modified threats usually exist in baseline
                    addedLibrary.Threats.Add(threat);
                }
                else if (deletedLibrary != null)
                {
                    // Library is in deleted list - threat was modified but library is being deleted
                    deletedLibrary.Threats.Add(threat);
                }
                else if (modifiedLibrary != null)
                {
                    // Library is in modified list - add to library's modified threats
                    modifiedLibrary.Threats.Modified.Add(modifiedEntity);
                }
                else
                {
                    // Library not found - create new modified library and add threat to modified
                    LibraryDrift newLibrary = CreateModifiedLibrary(libraryKey);
                    newLibrary.Threats.Modified.Add(modifiedEntity);
                    response.ModifiedLibraries.Add(newLibrary);
                }
            }
        }

        private void ProcessSecurityRequirements(CoreEntitiesDrift report, TMFrameworkDrift response, EntityDriftAggregationOptions cfg)
        {
            // Process added security requirements
            foreach (SecurityRequirement securityRequirement in report.SecurityRequirements.Added)
            {
                LibraryKey libraryKey = LibraryKey.From(securityRequirement);
                (AddedLibrary? addedLibrary, DeletedLibrary? deletedLibrary, LibraryDrift? modifiedLibrary) = FindLibraryInResponse(response, libraryKey);

                if (addedLibrary != null)
                {
                    // Library is in added list - add to library's added security requirements
                    addedLibrary.SecurityRequirements.Add(securityRequirement);
                }
                else if (deletedLibrary != null)
                {
                    // Library is in deleted list - add to library's deleted security requirements
                    deletedLibrary.SecurityRequirements.Add(securityRequirement);
                }
                else if (modifiedLibrary != null)
                {
                    // Library is in modified list - add to library's added security requirements
                    modifiedLibrary.SecurityRequirements.Added.Add(securityRequirement);
                }
                else
                {
                    // Library not found - create new modified library and add security requirement to added
                    LibraryDrift newLibrary = CreateModifiedLibrary(libraryKey);
                    newLibrary.SecurityRequirements.Added.Add(securityRequirement);
                    response.ModifiedLibraries.Add(newLibrary);
                }
            }

            // Process removed security requirements
            foreach (SecurityRequirement securityRequirement in report.SecurityRequirements.Removed)
            {
                LibraryKey libraryKey = LibraryKey.From(securityRequirement);
                (AddedLibrary? addedLibrary, DeletedLibrary? deletedLibrary, LibraryDrift? modifiedLibrary) = FindLibraryInResponse(response, libraryKey);

                if (addedLibrary != null)
                {
                    // Library is in added list - add to library's added security requirements (removed from baseline but library is new)
                    addedLibrary.SecurityRequirements.Add(securityRequirement);
                }
                else if (deletedLibrary != null)
                {
                    // Library is in deleted list - add to library's deleted security requirements
                    deletedLibrary.SecurityRequirements.Add(securityRequirement);
                }
                else if (modifiedLibrary != null)
                {
                    // Library is in modified list - add to library's removed security requirements
                    modifiedLibrary.SecurityRequirements.Removed.Add(securityRequirement);
                }
                else
                {
                    // Library not found - create new modified library and add security requirement to removed
                    LibraryDrift newLibrary = CreateModifiedLibrary(libraryKey);
                    newLibrary.SecurityRequirements.Removed.Add(securityRequirement);
                    response.ModifiedLibraries.Add(newLibrary);
                }
            }

            // Process modified security requirements
            foreach (EntityPair<SecurityRequirement> pair in report.SecurityRequirements.Modified)
            {
                SecurityRequirement securityRequirement = pair.Updated;
                LibraryKey libraryKey = LibraryKey.From(securityRequirement);
                List<FieldChange> changes = Compare(pair.Existing, pair.Updated, cfg.SecurityRequirementDefaultFields);

                if (changes.Count == 0)
                {
                    continue;
                }

                (AddedLibrary? addedLibrary, DeletedLibrary? deletedLibrary, LibraryDrift? modifiedLibrary) = FindLibraryInResponse(response, libraryKey);
                ModifiedEntity<SecurityRequirement> modifiedEntity = new()
                {
                    EntityKey = EntityKey.Of(securityRequirement),
                    ModifiedFields = changes
                };

                if (addedLibrary != null)
                {
                    // Library is in added list - security requirement modifications are part of the new library
                    // Note: This case might be rare as modified security requirements usually exist in baseline
                    addedLibrary.SecurityRequirements.Add(securityRequirement);
                }
                else if (deletedLibrary != null)
                {
                    // Library is in deleted list - security requirement was modified but library is being deleted
                    deletedLibrary.SecurityRequirements.Add(securityRequirement);
                }
                else if (modifiedLibrary != null)
                {
                    // Library is in modified list - add to library's modified security requirements
                    modifiedLibrary.SecurityRequirements.Modified.Add(modifiedEntity);
                }
                else
                {
                    // Library not found - create new modified library and add security requirement to modified
                    LibraryDrift newLibrary = CreateModifiedLibrary(libraryKey);
                    newLibrary.SecurityRequirements.Modified.Add(modifiedEntity);
                    response.ModifiedLibraries.Add(newLibrary);
                }
            }
        }

        private void ProcessTestCases(CoreEntitiesDrift report, TMFrameworkDrift response, EntityDriftAggregationOptions cfg)
        {
            // Process added test cases
            foreach (TestCase testCase in report.TestCases.Added)
            {
                LibraryKey libraryKey = LibraryKey.From(testCase);
                (AddedLibrary? addedLibrary, DeletedLibrary? deletedLibrary, LibraryDrift? modifiedLibrary) = FindLibraryInResponse(response, libraryKey);

                if (addedLibrary != null)
                {
                    // Library is in added list - add to library's added test cases
                    addedLibrary.TestCases.Add(testCase);
                }
                else if (deletedLibrary != null)
                {
                    // Library is in deleted list - add to library's deleted test cases
                    deletedLibrary.TestCases.Add(testCase);
                }
                else if (modifiedLibrary != null)
                {
                    // Library is in modified list - add to library's added test cases
                    modifiedLibrary.TestCases.Added.Add(testCase);
                }
                else
                {
                    // Library not found - create new modified library and add test case to added
                    LibraryDrift newLibrary = CreateModifiedLibrary(libraryKey);
                    newLibrary.TestCases.Added.Add(testCase);
                    response.ModifiedLibraries.Add(newLibrary);
                }
            }

            // Process removed test cases
            foreach (TestCase testCase in report.TestCases.Removed)
            {
                LibraryKey libraryKey = LibraryKey.From(testCase);
                (AddedLibrary? addedLibrary, DeletedLibrary? deletedLibrary, LibraryDrift? modifiedLibrary) = FindLibraryInResponse(response, libraryKey);

                if (addedLibrary != null)
                {
                    // Library is in added list - add to library's added test cases (removed from baseline but library is new)
                    addedLibrary.TestCases.Add(testCase);
                }
                else if (deletedLibrary != null)
                {
                    // Library is in deleted list - add to library's deleted test cases
                    deletedLibrary.TestCases.Add(testCase);
                }
                else if (modifiedLibrary != null)
                {
                    // Library is in modified list - add to library's removed test cases
                    modifiedLibrary.TestCases.Removed.Add(testCase);
                }
                else
                {
                    // Library not found - create new modified library and add test case to removed
                    LibraryDrift newLibrary = CreateModifiedLibrary(libraryKey);
                    newLibrary.TestCases.Removed.Add(testCase);
                    response.ModifiedLibraries.Add(newLibrary);
                }
            }

            // Process modified test cases
            foreach (EntityPair<TestCase> pair in report.TestCases.Modified)
            {
                TestCase testCase = pair.Updated;
                LibraryKey libraryKey = LibraryKey.From(testCase);
                List<FieldChange> changes = Compare(pair.Existing, pair.Updated, cfg.TestCaseDefaultFields);

                if (changes.Count == 0)
                {
                    continue;
                }

                (AddedLibrary? addedLibrary, DeletedLibrary? deletedLibrary, LibraryDrift? modifiedLibrary) = FindLibraryInResponse(response, libraryKey);
                ModifiedEntity<TestCase> modifiedEntity = new()
                {
                    EntityKey = EntityKey.Of(testCase),
                    ModifiedFields = changes
                };

                if (addedLibrary != null)
                {
                    // Library is in added list - test case modifications are part of the new library
                    // Note: This case might be rare as modified test cases usually exist in baseline
                    addedLibrary.TestCases.Add(testCase);
                }
                else if (deletedLibrary != null)
                {
                    // Library is in deleted list - test case was modified but library is being deleted
                    deletedLibrary.TestCases.Add(testCase);
                }
                else if (modifiedLibrary != null)
                {
                    // Library is in modified list - add to library's modified test cases
                    modifiedLibrary.TestCases.Modified.Add(modifiedEntity);
                }
                else
                {
                    // Library not found - create new modified library and add test case to modified
                    LibraryDrift newLibrary = CreateModifiedLibrary(libraryKey);
                    newLibrary.TestCases.Modified.Add(modifiedEntity);
                    response.ModifiedLibraries.Add(newLibrary);
                }
            }
        }

        private void ProcessProperties(CoreEntitiesDrift report, TMFrameworkDrift response, EntityDriftAggregationOptions cfg)
        {
            // Process added properties
            foreach (Property property in report.Properties.Added)
            {
                LibraryKey libraryKey = LibraryKey.From(property);
                (AddedLibrary? addedLibrary, DeletedLibrary? deletedLibrary, LibraryDrift? modifiedLibrary) = FindLibraryInResponse(response, libraryKey);

                if (addedLibrary != null)
                {
                    // Library is in added list - add to library's added properties
                    addedLibrary.Properties.Add(property);
                }
                else if (deletedLibrary != null)
                {
                    // Library is in deleted list - add to library's deleted properties
                    deletedLibrary.Properties.Add(property);
                }
                else if (modifiedLibrary != null)
                {
                    // Library is in modified list - add to library's added properties
                    modifiedLibrary.Properties.Added.Add(property);
                }
                else
                {
                    // Library not found - create new modified library and add property to added
                    LibraryDrift newLibrary = CreateModifiedLibrary(libraryKey);
                    newLibrary.Properties.Added.Add(property);
                    response.ModifiedLibraries.Add(newLibrary);
                }
            }

            // Process removed properties
            foreach (Property property in report.Properties.Removed)
            {
                LibraryKey libraryKey = LibraryKey.From(property);
                (AddedLibrary? addedLibrary, DeletedLibrary? deletedLibrary, LibraryDrift? modifiedLibrary) = FindLibraryInResponse(response, libraryKey);

                if (addedLibrary != null)
                {
                    // Library is in added list - add to library's added properties (removed from baseline but library is new)
                    addedLibrary.Properties.Add(property);
                }
                else if (deletedLibrary != null)
                {
                    // Library is in deleted list - add to library's deleted properties
                    deletedLibrary.Properties.Add(property);
                }
                else if (modifiedLibrary != null)
                {
                    // Library is in modified list - add to library's removed properties
                    modifiedLibrary.Properties.Removed.Add(property);
                }
                else
                {
                    // Library not found - create new modified library and add property to removed
                    LibraryDrift newLibrary = CreateModifiedLibrary(libraryKey);
                    newLibrary.Properties.Removed.Add(property);
                    response.ModifiedLibraries.Add(newLibrary);
                }
            }

            // Process modified properties
            foreach (EntityPair<Property> pair in report.Properties.Modified)
            {
                Property property = pair.Updated;
                LibraryKey libraryKey = LibraryKey.From(property);
                List<FieldChange> changes = Compare(pair.Existing, pair.Updated, cfg.PropertyDefaultFields);

                if (changes.Count == 0)
                {
                    continue;
                }

                (AddedLibrary? addedLibrary, DeletedLibrary? deletedLibrary, LibraryDrift? modifiedLibrary) = FindLibraryInResponse(response, libraryKey);
                ModifiedEntity<Property> modifiedEntity = new()
                {
                    EntityKey = EntityKey.Of(property),
                    ModifiedFields = changes
                };

                if (addedLibrary != null)
                {
                    // Library is in added list - property modifications are part of the new library
                    // Note: This case might be rare as modified properties usually exist in baseline
                    addedLibrary.Properties.Add(property);
                }
                else if (deletedLibrary != null)
                {
                    // Library is in deleted list - property was modified but library is being deleted
                    deletedLibrary.Properties.Add(property);
                }
                else if (modifiedLibrary != null)
                {
                    // Library is in modified list - add to library's modified properties
                    modifiedLibrary.Properties.Modified.Add(modifiedEntity);
                }
                else
                {
                    // Library not found - create new modified library and add property to modified
                    LibraryDrift newLibrary = CreateModifiedLibrary(libraryKey);
                    newLibrary.Properties.Modified.Add(modifiedEntity);
                    response.ModifiedLibraries.Add(newLibrary);
                }
            }
        }

        private (AddedLibrary? addedLibrary, DeletedLibrary? deletedLibrary, LibraryDrift? modifiedLibrary)
            FindLibraryInResponse(TMFrameworkDrift response, LibraryKey libraryKey)
        {
            AddedLibrary? addedLibrary = response.AddedLibraries.FirstOrDefault(l => l.Library.Guid == libraryKey.Guid);
            DeletedLibrary? deletedLibrary = response.DeletedLibraries.FirstOrDefault(l => l.LibraryGuid == libraryKey.Guid);
            LibraryDrift? modifiedLibrary = response.ModifiedLibraries.FirstOrDefault(l => l.LibraryGuid == libraryKey.Guid);

            return (addedLibrary, deletedLibrary, modifiedLibrary);
        }

        private LibraryDrift CreateModifiedLibrary(LibraryKey libraryKey)
        {
            return new LibraryDrift
            {
                LibraryGuid = libraryKey.Guid,
                LibraryName = libraryKey.Name
            };
        }

        private static List<FieldChange> Compare<T>(T existing, T updated, IEnumerable<string> fields)
          where T : class
        {
            if (existing is IFieldComparable<T> cmp1)
            {
                return cmp1.CompareFields(updated, fields).ToList();
            }

            if (updated is IFieldComparable<T> cmp2)
            {
                return cmp2.CompareFields(existing, fields).ToList(); // same semantics: compare sets
            }

            // Fallback: strict reflection-based compare
            return FieldComparer.CompareByNames(existing, updated, fields).ToList();
        }

        public Task<TMFrameworkDrift> Drift(IEnumerable<Guid> libraryIds)
        {
            throw new NotImplementedException();
        }

        private class ComponentCollections
        {
            public Dictionary<Guid, AddedComponent> Added { get; } = [];
            public Dictionary<Guid, DeletedComponent> Deleted { get; } = [];
            public Dictionary<Guid, ModifiedComponent> Modified { get; } = [];
        }
    }
}