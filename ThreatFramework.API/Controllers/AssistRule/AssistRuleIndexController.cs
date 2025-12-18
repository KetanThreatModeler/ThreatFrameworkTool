using Microsoft.AspNetCore.Mvc;
using ThreatFramework.Infra.Contract;
using ThreatModeler.TF.API.Controllers.Dtos;
using ThreatModeler.TF.Drift.Contract; // ILibraryCacheService
using ThreatModeler.TF.Infra.Contract.AssistRuleIndex.Model;
using ThreatModeler.TF.Infra.Contract.AssistRuleIndex.Service;

namespace ThreatModeler.TF.API.Controllers
{
    [ApiController]
    [Route("api/assist-rules/index")]
    public sealed class AssistRuleIndexController : ControllerBase
    {
        private readonly IAssistRuleIndexManager _manager;
        private readonly IAssistRuleIndexQuery _query;
        private readonly ILibraryCacheService _libraryCacheService;
        private readonly ILogger<AssistRuleIndexController> _logger;

        public AssistRuleIndexController(
            IAssistRuleIndexManager manager,
            IAssistRuleIndexQuery query,
            ILibraryCacheService libraryCacheService,
            ILogger<AssistRuleIndexController> logger)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            _query = query ?? throw new ArgumentNullException(nameof(query));
            _libraryCacheService = libraryCacheService ?? throw new ArgumentNullException(nameof(libraryCacheService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // ----------------------------
        // Existing endpoints (manual GUIDs supported)
        // ----------------------------

        /// <summary>
        /// Build index in-memory.
        /// If LibraryGuids provided, filters ResourceTypeValues by those libraries (Relationships always included).
        /// </summary>
        [HttpPost("build")]
        [ProducesResponseType(typeof(IReadOnlyList<AssistRuleIndexEntry>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Build([FromBody] AssistRuleIndexWriteRequest request)
        {
            _logger.LogInformation(
                "API: Build AssistRules index (manual). Filtered libraries count: {Count}",
                request.LibraryGuids?.Count ?? 0);

            var entries = await _manager.BuildAsync(request.LibraryGuids);
            return Ok(entries);
        }

        /// <summary>
        /// Build index and write YAML to disk, also updates in-memory cache.
        /// </summary>
        [HttpPost("build-and-write")]
        [ProducesResponseType(typeof(IReadOnlyList<AssistRuleIndexEntry>), StatusCodes.Status200OK)]
        public async Task<IActionResult> BuildAndWrite([FromBody] AssistRuleIndexWriteRequest request)
        {
            _logger.LogInformation(
                "API: Build+Write AssistRules index (manual). Filtered libraries count: {Count}",
                request.LibraryGuids?.Count ?? 0);

            var entries = await _manager.BuildAndWriteAsync(request.LibraryGuids);
            return Ok(entries);
        }

        /// <summary>
        /// Reload index from YAML and update in-memory cache.
        /// </summary>
        [HttpPost("reload")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Reload()
        {
            _logger.LogInformation("API: Reload AssistRules index from YAML.");
            await _manager.ReloadFromYamlAsync();
            return NoContent();
        }

        /// <summary>
        /// Get all cached index entries.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IReadOnlyList<AssistRuleIndexEntry>), StatusCodes.Status200OK)]
        public IActionResult GetAll()
        {
            return Ok(_query.GetAll());
        }

        /// <summary>
        /// Lookup ID for a Relationship GUID.
        /// </summary>
        [HttpGet("relationship/{relationshipGuid:guid}/id")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult GetIdByRelationshipGuid([FromRoute] Guid relationshipGuid)
        {
            if (_query.TryGetIdByRelationshipGuid(relationshipGuid, out var id))
                return Ok(id);

            return NotFound();
        }

        /// <summary>
        /// Lookup ID for a ResourceTypeValue identity (string).
        /// </summary>
        [HttpGet("resource-type-value/id")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult GetIdByResourceTypeValue([FromQuery] string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return BadRequest("Query parameter 'value' is required.");

            if (_query.TryGetIdByResourceTypeValue(value, out var id))
                return Ok(id);

            return NotFound();
        }

        /// <summary>
        /// Get all ResourceTypeValues index entries for a given library.
        /// </summary>
        [HttpGet("resource-type-values/library/{libraryGuid:guid}")]
        [ProducesResponseType(typeof(IReadOnlyList<AssistRuleIndexEntry>), StatusCodes.Status200OK)]
        public IActionResult GetResourceTypeValuesByLibrary([FromRoute] Guid libraryGuid)
        {
            return Ok(_query.GetResourceTypeValuesByLibraryGuid(libraryGuid));
        }

        // ----------------------------
        // NEW endpoints (no GUIDs from client; uses readonly libraries)
        // ----------------------------

        /// <summary>
        /// Build index in-memory using readonly library GUIDs from cache.
        /// </summary>
        [HttpPost("build/readonly")]
        [ProducesResponseType(typeof(IReadOnlyList<AssistRuleIndexEntry>), StatusCodes.Status200OK)]
        public async Task<IActionResult> BuildReadonly()
        {
            var readOnlyLibraryGuids = await GetReadonlyLibraryGuidsAsync();

            // If count==0, pass null to mean "no filter" (build everything)
            var entries = await _manager.BuildAsync(readOnlyLibraryGuids);
            return Ok(entries);
        }

        /// <summary>
        /// Build index using readonly library GUIDs and write YAML to disk (regenerate file).
        /// Also updates in-memory cache.
        /// </summary>
        [HttpPost("build-and-write/readonly")]
        [ProducesResponseType(typeof(IReadOnlyList<AssistRuleIndexEntry>), StatusCodes.Status200OK)]
        public async Task<IActionResult> BuildAndWriteReadonly()
        {
            var readOnlyLibraryGuids = await GetReadonlyLibraryGuidsAsync();


            var entries = await _manager.BuildAndWriteAsync(readOnlyLibraryGuids);
            return Ok(entries);
        }

        // ----------------------------
        // Helper
        // ----------------------------

        private async Task<IEnumerable<Guid>> GetReadonlyLibraryGuidsAsync()
        {
            _logger.LogInformation("Refreshing library cache to fetch readonly library GUIDs...");
            await _libraryCacheService.RefreshCacheAsync();

            var guids = await _libraryCacheService.GetReadonlyLibraryGuidsAsync();
            return guids;
        }
    }
}
