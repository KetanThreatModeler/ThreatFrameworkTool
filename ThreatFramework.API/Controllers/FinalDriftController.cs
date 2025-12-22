using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ThreatFramework.Infra.Contract;
using ThreatFramework.Infra.Contract.Index;
using ThreatModeler.TF.API.Controllers.Dtos;
using ThreatModeler.TF.Core.Model.CoreEntities;
using ThreatModeler.TF.Drift.Contract;
using ThreatModeler.TF.Drift.Contract.Dto;
using ThreatModeler.TF.Drift.Contract.Model;

namespace ThreatModeler.TF.Drift.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DriftController : ControllerBase
    {
        private readonly IDriftService _finalDriftService;
        private readonly IGuidIndexService _guidIndexService;
        private readonly ILibraryCacheService _libraryCacheService;
        private readonly PathOptions _pathOptions;
        private readonly ILogger<DriftController> _logger;
        private readonly ILibraryChangeSummaryMapper _libraryChangeSummaryMapper;

        public DriftController(
            IDriftService finalDriftService,
            IGuidIndexService guidIndexService,
            ILibraryCacheService libraryCacheService,
            IOptions<PathOptions> pathOptions,
            ILibraryChangeSummaryMapper libraryChangeSummaryMapper,
            ILogger<DriftController> logger)
        {
            _finalDriftService = finalDriftService ?? throw new ArgumentNullException(nameof(finalDriftService));
            _guidIndexService = guidIndexService ?? throw new ArgumentNullException(nameof(guidIndexService));
            _libraryCacheService = libraryCacheService ?? throw new ArgumentNullException(nameof(libraryCacheService));
            _pathOptions = pathOptions?.Value ?? throw new ArgumentNullException(nameof(pathOptions));
            _libraryChangeSummaryMapper = libraryChangeSummaryMapper ?? throw new ArgumentNullException(nameof(libraryChangeSummaryMapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpPost("readonly")]
        [ProducesResponseType(typeof(TMFrameworkDriftDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> CalculateReadonlyDriftAsync(
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting readonly drift calculation: refreshing GUID index...");

            // 👇 Pass a valid path instead of null
            // Use TrcOutput or ClientOutput depending on where your index lives
            await _guidIndexService.RefreshAsync(_pathOptions.IndexYaml);
            _logger.LogInformation("GUID index refresh completed.");

            _logger.LogInformation("Refreshing library cache...");
            await _libraryCacheService.RefreshCacheAsync();
            _logger.LogInformation("Library cache refresh completed.");

            _logger.LogInformation("Fetching readonly library GUIDs from cache...");
            var readOnlyLibraryGuids = await _libraryCacheService.GetReadonlyLibraryGuidsAsync();

            if (readOnlyLibraryGuids == null || readOnlyLibraryGuids.Count == 0)
            {
                _logger.LogInformation("No readonly libraries found. Returning empty drift result.");
                return Ok(new TMFrameworkDriftDto());
            }


            var drift = await _finalDriftService.DriftAsync(readOnlyLibraryGuids, cancellationToken);

            _logger.LogInformation("Readonly drift calculation completed.");

            _logger.LogInformation(drift.ToString());
            return Ok(drift);
        }

           
            // New v2 endpoint
            [HttpGet("threatFramework/drift")]
            [ProducesResponseType(typeof(TMFrameworkDrift), StatusCodes.Status200OK)]
            public async Task<IActionResult> CalculateReadonlyDriftV2Async(CancellationToken cancellationToken)
            {
                _logger.LogInformation("Starting readonly drift V2 calculation: refreshing GUID index...");
                await _guidIndexService.RefreshAsync(_pathOptions.IndexYaml);

                _logger.LogInformation("Refreshing library cache...");
                await _libraryCacheService.RefreshCacheAsync();

                var readOnlyLibraryGuids = await _libraryCacheService.GetReadonlyLibraryGuidsAsync();
                if (readOnlyLibraryGuids == null || readOnlyLibraryGuids.Count == 0)
                    return Ok(new TMFrameworkDrift());

                var drift = await _finalDriftService.DriftAsync1(readOnlyLibraryGuids, cancellationToken);
                return Ok(drift);
            }

        [HttpGet("threatFramework/drift/library-changes")]
        [ProducesResponseType(typeof(IReadOnlyList<LibraryChangeSummaryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetReadonlyDriftLibraryChangesAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting readonly drift library changes: refreshing GUID index...");
            await _guidIndexService.RefreshAsync(_pathOptions.IndexYaml);

            _logger.LogInformation("Refreshing library cache...");
            await _libraryCacheService.RefreshCacheAsync();

            var readOnlyLibraryGuids = await _libraryCacheService.GetReadonlyLibraryGuidsAsync();
           

            var drift = await _finalDriftService.DriftAsync1(readOnlyLibraryGuids, cancellationToken);
            var summary = _libraryChangeSummaryMapper.Map(drift);

            return Ok(ApiResponseModel<IReadOnlyList<LibraryChangeSummaryDto>>.Success(summary));
        }
    }
}
