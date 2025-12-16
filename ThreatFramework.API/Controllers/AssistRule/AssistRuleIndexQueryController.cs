using Microsoft.AspNetCore.Mvc;
using ThreatFramework.Infra.Contract;
using ThreatModeler.TF.Infra.Contract.AssistRuleIndex.Model;
using ThreatModeler.TF.Infra.Contract.AssistRuleIndex.Service;

namespace ThreatModeler.TF.API.Controllers.AssistRule
{
    [ApiController]
    [Route("api/assist-rules/index/query")]
    public sealed class AssistRuleIndexQueryController : ControllerBase
    {
        private readonly IAssistRuleIndexQuery _query;
        private readonly ILibraryCacheService? _libraryCacheService;
        private readonly ILogger<AssistRuleIndexQueryController> _logger;

        public AssistRuleIndexQueryController(
            IAssistRuleIndexQuery query,
            ILogger<AssistRuleIndexQueryController> logger,
            ILibraryCacheService? libraryCacheService = null) // optional
        {
            _query = query ?? throw new ArgumentNullException(nameof(query));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _libraryCacheService = libraryCacheService;
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

        /// <summary>
        /// Get all ResourceTypeValues index entries for readonly libraries (no GUID required).
        /// Requires ILibraryCacheService to be registered/injected.
        /// </summary>
        [HttpGet("resource-type-values/readonly")]
        [ProducesResponseType(typeof(IReadOnlyList<AssistRuleIndexEntry>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status501NotImplemented)]
        public async Task<IActionResult> GetResourceTypeValuesForReadonlyLibraries()
        {
            if (_libraryCacheService is null)
                return StatusCode(StatusCodes.Status501NotImplemented,
                    "ILibraryCacheService is not configured for this controller.");

            _logger.LogInformation("Refreshing library cache and retrieving readonly library GUIDs...");
            await _libraryCacheService.RefreshCacheAsync();

            var readonlyGuids = await _libraryCacheService.GetReadonlyLibraryGuidsAsync();
            if (readonlyGuids == null || readonlyGuids.Count == 0)
                return Ok(Array.Empty<AssistRuleIndexEntry>());

            var all = new List<AssistRuleIndexEntry>();
            foreach (var libGuid in readonlyGuids)
                all.AddRange(_query.GetResourceTypeValuesByLibraryGuid(libGuid));

            return Ok(all);
        }
    }
}