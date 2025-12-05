using Microsoft.Extensions.Options;
using ThreatFramework.Core.ComponentMapping;
using ThreatFramework.Core.PropertyMapping;
using ThreatFramework.Drift.Contract.MappingDriftService;
using ThreatFramework.Drift.Contract.MappingDriftService.Builder;
using ThreatFramework.Drift.Contract.MappingDriftService.Dto;
using ThreatFramework.Drift.Contract.MappingDriftService.Model;
using ThreatFramework.Infra.Contract.Repository;
using ThreatFramework.Infra.Contract.YamlRepository;
using ThreatModeler.TF.Core.CoreEntities;

namespace ThreatFramework.Drift.Impl.MappingDriftService
{
    public class ComponentMappingDriftService : IComponentMappingDriftService
    {
        private readonly IComponentSecurityRequirementMappingRepository _componentSecurityRequirementMappingRepository;
        private readonly IComponentThreatMappingRepository _componentThreatMappingRepository;
        private readonly IComponentThreatSecurityRequirementMappingRepository _componentThreatSecurityRequirementMappingRepository;
        private readonly IComponentPropertyMappingRepository _componentPropertyMappingRepository;
        private readonly IComponentPropertyOptionMappingRepository _componentPropertyOptionMappingRepository;
        private readonly IComponentPropertyOptionThreatMappingRepository _componentPropertyOptionThreatMappingRepository;
        private readonly IComponentPropertyOptionThreatSecurityRequirementMappingRepository _componentPropertyOptionThreatSecurityRequirementMappingRepository;
        private readonly IYamlComponentSRReader _yamlComponentSRReader;
        private readonly IYamlComponentThreatReader _yamlComponentThreatReader;
        private readonly IYamlComponentThreatSRReader _yamlComponentThreatSRReader;
        private readonly IYamlComponentPropertyReader _yamlComponentPropertyReader;
        private readonly IYamlComponentPropertyOptionReader _yamlComponentPropertyOptionReader;
        private readonly IYamlComponentPropertyOptionThreatReader _yamlComponentPropertyOptionThreatReader;
        private readonly IYamlCpoThreatSrReader _yamlCpoThreatSrReader;
        private readonly IComponentPropertyGraphBuilder _componentPropertyGraphBuilder;
        private readonly IComponentThreatSRGraphBuilder _componentThreatGraphBuilder;
        private readonly IComponentSRGraphBuilder _componentSRGraphBuilder;
        private readonly IComponentPropertyMappingDriftService _componentPropertyMappingDriftService;
        private readonly IComponentThreatSRDriftService _componentThreatDriftService;
        private readonly IComponentSRDriftService _componentSrDriftService;
        private readonly IComponentMappingDriftAggregator _componentMappingDriftComposer;
        private readonly PathOptions _exportOptions;



        public ComponentMappingDriftService(IComponentSRDriftService componentSrDriftService,
                                   IComponentSecurityRequirementMappingRepository componentSecurityRequirementMappingRepository,
                                   IComponentThreatMappingRepository componentThreatMappingRepository,
                                   IComponentThreatSecurityRequirementMappingRepository componentThreatSecurityRequirementMappingRepository,
                                   IComponentPropertyMappingRepository componentPropertyMappingRepository,
                                   IComponentPropertyOptionMappingRepository componentPropertyOptionMappingRepository,
                                   IComponentPropertyOptionThreatMappingRepository componentPropertyOptionThreatMappingRepository,
                                   IComponentPropertyOptionThreatSecurityRequirementMappingRepository componentPropertyOptionThreatSecurityRequirementMappingRepository,
                                   IYamlComponentSRReader yamlComponentSRReader,
                                   IYamlComponentThreatReader yamlComponentThreatReader,
                                   IYamlComponentThreatSRReader yamlComponentThreatSRReader,
                                   IYamlComponentPropertyReader yamlComponentPropertyReader,
                                   IYamlComponentPropertyOptionReader yamlComponentPropertyOptionReader,
                                   IYamlComponentPropertyOptionThreatReader yamlComponentPropertyOptionThreatReader,
                                   IYamlCpoThreatSrReader yamlCpoThreatSrReader,
                                   IComponentPropertyGraphBuilder componentPropertyGraphBuilder,
                                   IComponentThreatSRGraphBuilder componentThreatGraphBuilder,
                                   IComponentSRGraphBuilder componentSRGraphBuilder,
                                   IComponentPropertyMappingDriftService componentPropertyMappingDriftService,
                                   IComponentThreatSRDriftService componentThreatDriftService,
                                   IComponentSRDriftService componentSrDrift,
                                   IComponentMappingDriftAggregator componentMappingDriftComposer,
                                   IOptions<PathOptions> exportOptions)
        {
            _componentSecurityRequirementMappingRepository = componentSecurityRequirementMappingRepository ?? throw new ArgumentNullException(nameof(componentSecurityRequirementMappingRepository));
            _componentThreatMappingRepository = componentThreatMappingRepository ?? throw new ArgumentNullException(nameof(componentThreatMappingRepository));
            _componentThreatSecurityRequirementMappingRepository = componentThreatSecurityRequirementMappingRepository ?? throw new ArgumentNullException(nameof(componentThreatSecurityRequirementMappingRepository));
            _componentPropertyMappingRepository = componentPropertyMappingRepository ?? throw new ArgumentNullException(nameof(componentPropertyMappingRepository));
            _componentPropertyOptionMappingRepository = componentPropertyOptionMappingRepository ?? throw new ArgumentNullException(nameof(componentPropertyOptionMappingRepository));
            _componentPropertyOptionThreatMappingRepository = componentPropertyOptionThreatMappingRepository ?? throw new ArgumentNullException(nameof(componentPropertyOptionThreatMappingRepository));
            _componentPropertyOptionThreatSecurityRequirementMappingRepository = componentPropertyOptionThreatSecurityRequirementMappingRepository ?? throw new ArgumentNullException(nameof(componentPropertyOptionThreatSecurityRequirementMappingRepository));
            _yamlComponentSRReader = yamlComponentSRReader ?? throw new ArgumentNullException(nameof(yamlComponentSRReader));
            _yamlComponentThreatReader = yamlComponentThreatReader ?? throw new ArgumentNullException(nameof(yamlComponentThreatReader));
            _yamlComponentThreatSRReader = yamlComponentThreatSRReader ?? throw new ArgumentNullException(nameof(yamlComponentThreatSRReader));
            _yamlComponentPropertyReader = yamlComponentPropertyReader ?? throw new ArgumentNullException(nameof(yamlComponentPropertyReader));
            _yamlComponentPropertyOptionReader = yamlComponentPropertyOptionReader ?? throw new ArgumentNullException(nameof(yamlComponentPropertyOptionReader));
            _yamlComponentPropertyOptionThreatReader = yamlComponentPropertyOptionThreatReader ?? throw new ArgumentNullException(nameof(yamlComponentPropertyOptionThreatReader));
            _yamlCpoThreatSrReader = yamlCpoThreatSrReader ?? throw new ArgumentNullException(nameof(yamlCpoThreatSrReader));
            _componentPropertyGraphBuilder = componentPropertyGraphBuilder ?? throw new ArgumentNullException(nameof(componentPropertyGraphBuilder));
            _componentSRGraphBuilder = componentSRGraphBuilder ?? throw new ArgumentNullException(nameof(componentSRGraphBuilder));
            _componentThreatGraphBuilder = componentThreatGraphBuilder ?? throw new ArgumentNullException(nameof(componentThreatGraphBuilder));
            _componentPropertyMappingDriftService = componentPropertyMappingDriftService ?? throw new ArgumentNullException(nameof(componentPropertyMappingDriftService));
            _componentThreatDriftService = componentThreatDriftService ?? throw new ArgumentNullException(nameof(componentThreatDriftService));
            _componentSrDriftService = componentSrDriftService ?? throw new ArgumentNullException(nameof(componentSrDriftService));
            _componentMappingDriftComposer = componentMappingDriftComposer ?? throw new ArgumentNullException(nameof(componentMappingDriftComposer));
            _exportOptions = exportOptions?.Value ?? throw new ArgumentNullException(nameof(exportOptions));
        }


        public async Task<IEnumerable<ComponentMappingDriftDto>> GetMappingDrift()
        {
            var drift = new List<ComponentMappingDriftDto>();
            var cpoThreatSrMappingFromSourceA = await _componentPropertyOptionThreatSecurityRequirementMappingRepository.GetReadOnlyMappingsAsync();
            var cpoThreatMappingFromSourceA = await _componentPropertyOptionThreatMappingRepository.GetReadOnlyMappingsAsync();
            var cpoMappingFromSourceA = await _componentPropertyOptionMappingRepository.GetReadOnlyMappingsAsync();
            var propertyMappingFromSourceA = await _componentPropertyMappingRepository.GetReadOnlyMappingsAsync();

            var cpoThreatSrMappingFromSourceB = await _yamlCpoThreatSrReader.GetAllAsync(_exportOptions.TrcOutput);
            var cpoThreatMappingFromSourceB = await _yamlComponentPropertyOptionThreatReader.GetAllAsync(_exportOptions.TrcOutput);
            var cpoMappingFromSourceB = await _yamlComponentPropertyOptionReader.GetAllAsync(_exportOptions.TrcOutput);
            var propertyMappingFromSourceB = await _yamlComponentPropertyReader.GetAllAsync(_exportOptions.TrcOutput);

            var sourceA = ToMapping(cpoThreatSrMappingFromSourceA.ToList(),
                                  cpoThreatMappingFromSourceA.ToList(),
                                  cpoMappingFromSourceA.ToList(),
                                  propertyMappingFromSourceA.ToList());

            var sourceB = ToMapping(cpoThreatSrMappingFromSourceB.ToList(),
                                    cpoThreatMappingFromSourceB.ToList(),
                                    cpoMappingFromSourceB.ToList(),
                                    propertyMappingFromSourceB.ToList());

            var componentPropertyDrift = _componentPropertyMappingDriftService.ComputeDrift(sourceA, sourceB);

            var cpomponentThreatSrMappingFromSourceA = await _componentThreatSecurityRequirementMappingRepository.GetReadOnlyMappingsAsync();
            var componentThreatMappingFromSourceA = await _componentThreatMappingRepository.GetReadOnlyMappingsAsync();


            var componentThreatSrMappingFromSourceB = await _yamlComponentThreatSRReader.GetAllAsync(_exportOptions.TrcOutput);
            var componentThreatMappingFromSourceB = await _yamlComponentThreatReader.GetAllAsync(_exportOptions.TrcOutput);

            var sourceAComponentThreatSrMapping = ToMapping(componentThreatMappingFromSourceA.ToList(),
                                          cpomponentThreatSrMappingFromSourceA.ToList());
            var sourceBComponentThreatSrMapping = ToMapping(componentThreatMappingFromSourceB.ToList(),
                                          componentThreatSrMappingFromSourceB.ToList());

            var componentThreatDrift = _componentThreatDriftService.ComputeDrift(sourceAComponentThreatSrMapping, sourceBComponentThreatSrMapping);

            var componentSrMappingFromSourceA = await _componentSecurityRequirementMappingRepository.GetReadOnlyMappingsAsync();

            var componentSrMappingFromSourceB = await _yamlComponentSRReader.GetAllComponentSRAsync(_exportOptions.ClientOutput);

            var componetSrSourceA = ToMapping(componentSrMappingFromSourceA.ToList());
            var componentSrSourceB = ToMapping(componentSrMappingFromSourceB.ToList());

            var componentSrDrift = _componentSrDriftService.ComputeDrift(componetSrSourceA, componentSrSourceB);

            return _componentMappingDriftComposer.Compose(componentPropertyDrift, componentThreatDrift, componentSrDrift);
        }

        private ComponentPropertyGraph ToMapping(List<ComponentPropertyOptionThreatSecurityRequirementMapping> componentPropertyOptionThreatSecurityRequirementMappings,
           List<ComponentPropertyOptionThreatMapping> componentPropertyOptionThreatMappings,
           List<ComponentPropertyOptionMapping> componentPropertyOptionMappings,
           List<ComponentPropertyMapping> compo)
        {
            return _componentPropertyGraphBuilder.Build(
                 componentPropertyOptionThreatSecurityRequirementMappings,
                 componentPropertyOptionThreatMappings,
                 componentPropertyOptionMappings,
                 compo);
        }

        private ComponentThreatSRGraph ToMapping(
            List<ComponentThreatMapping> componentThreatMappings,
            List<ComponentThreatSecurityRequirementMapping> componentThreatSecurityRequirementMappings)
        {
            return _componentThreatGraphBuilder.Build(componentThreatSecurityRequirementMappings,
                componentThreatMappings);
        }

        private ComponentSRGraph ToMapping(
            List<ComponentSecurityRequirementMapping> componentSecurityRequirementMappings)
        {
            return _componentSRGraphBuilder.Build(componentSecurityRequirementMappings);
        }
    }
}
