using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using ThreatFramework.Infra.Contract.Index;
using ThreatModeler.TF.Core.Config;

namespace ThreatFramework.API.Controllers
{
    [ApiController]
    [Route("api/guid-index")]
    [Produces("application/json")]
    public sealed class GuidIndexController : ControllerBase
    {
        private readonly IGuidIndexService _service;
        private readonly PathOptions _pathOptions;
        private readonly ILogger<GuidIndexController> _logger;

        public GuidIndexController(
            IGuidIndexService service,
            IOptions<PathOptions> options,
            ILogger<GuidIndexController> logger)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Validate Options early
            _pathOptions = options?.Value ?? throw new ArgumentNullException(nameof(options));
            if (string.IsNullOrWhiteSpace(_pathOptions.IndexYaml))
            {
                throw new ArgumentException("Configuration error: 'IndexYaml' path is missing in PathOptions.");
            }
        }

        /// <summary>
        /// Generates the Global Index for all entities.
        /// Warning: This operation updates the global application cache.
        /// </summary>
        [HttpPost("generate")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GenerateGlobalAsync(CancellationToken ct)
        {
            using (_logger.BeginScope("Action: GenerateGlobalIndex"))
            {
                try
                {
                    _logger.LogInformation("Received request to generate global index.");

                    await _service.GenerateAsync(_pathOptions.IndexYaml);

                    _logger.LogInformation("Global index generated successfully at {Path}.", _pathOptions.IndexYaml);
                    return Ok(new { message = "Global index generated.", path = _pathOptions.IndexYaml });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to generate global index.");
                    return Problem(statusCode: 500, title: "Generation Failed", detail: ex.Message);
                }
            }
        }

        /// <summary>
        /// Generates an index specific to a list of Libraries.
        /// Note: This does NOT update the global application cache (to prevent data loss).
        /// </summary>
        [HttpPost("generate-library")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GenerateForLibraryAsync(
          IEnumerable<Guid> libraryIds,
            CancellationToken ct)
        {
            using (_logger.BeginScope("Action: GenerateLibraryIndex"))
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                try
                {
                    _logger.LogInformation("Received request to generate index for {Count} libraries.", libraryIds.Count());

                    // We use the same configured path, or you could derive a library-specific path strategy here.
                    // For now, using the configured path implies overwriting the file with a partial index.
                    // CAUTION: Ensure this is the intended behavior.
                    await _service.GenerateForLibraryAsync(libraryIds, _pathOptions.IndexYaml);

                    _logger.LogInformation("Library index generated successfully.");
                    return Ok(new { message = "Library index generated.", path = _pathOptions.IndexYaml });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to generate library index.");
                    return Problem(statusCode: 500, title: "Library Generation Failed", detail: ex.Message);
                }
            }
        }

        /// <summary>
        /// Forces a refresh of the in-memory cache from the file on disk.
        /// </summary>
        [HttpPost("refresh")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RefreshAsync()
        {
            using (_logger.BeginScope("Action: RefreshIndex"))
            {
                try
                {
                    _logger.LogInformation("Received request to refresh cache from disk.");

                    await _service.RefreshAsync(_pathOptions.IndexYaml);

                    _logger.LogInformation("Cache refreshed successfully.");
                    return Ok(new { message = "Cache refreshed.", path = _pathOptions.IndexYaml });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to refresh index cache.");
                    return Problem(statusCode: 500, title: "Refresh Failed", detail: ex.Message);
                }
            }
        }

        /// <summary>
        /// Retrieves the integer ID for a given GUID.
        /// </summary>
        [HttpGet("{guid:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult Get([FromRoute] Guid guid)
        {
            // Note: Removed 'async Task' wrapper because _service.GetInt is synchronous.
            // Using async here would just add state-machine overhead.

            try
            {
                // No need for heavy logging here as this might be a high-frequency endpoint.
                // Debug level is appropriate.
                _logger.LogDebug("Looking up ID for GUID: {Guid}", guid);

                var id = _service.GetInt(guid);

                return Ok(new { Guid = guid, Id = id });
            }
            catch (KeyNotFoundException)
            {
                _logger.LogWarning("GUID {Guid} not found in index.", guid);
                return NotFound(new { error = "GUID not found", guid });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error looking up GUID {Guid}", guid);
                return Problem(statusCode: 500, title: "Lookup Failed", detail: ex.Message);
            }
        }

        /// <summary>
        /// Retrieves the GUID for a given integer ID.
        /// </summary>
        [HttpGet("id/{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetGuid([FromRoute] int id)
        {
            try
            {
                _logger.LogDebug("Looking up GUID for ID: {Id}", id);

                var guid = _service.GetGuid(id);

                return Ok(new { Id = id, Guid = guid });
            }
            catch (KeyNotFoundException)
            {
                _logger.LogWarning("ID {Id} not found in index.", id);
                return NotFound(new { error = "ID not found", id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error looking up ID {Id}", id);
                return Problem(statusCode: 500, title: "Lookup Failed", detail: ex.Message);
            }
        }

        /// <summary>
        /// Retrieves all integer IDs for a given Library and EntityType.
        /// </summary>
        [HttpGet("library/{libraryId:guid}/type/{entityType}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetIdsByLibraryAndType(
            [FromRoute] Guid libraryId,
            [FromRoute] EntityType entityType)
        {
            try
            {
                _logger.LogDebug(
                    "Looking up IDs for Library {LibraryId} and EntityType {EntityType}",
                    libraryId, entityType);

                var ids = _service.GetIdsByLibraryAndType(libraryId, entityType);

                return Ok(new { LibraryId = libraryId, EntityType = entityType, Ids = ids });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unexpected error looking up IDs for Library {LibraryId} and EntityType {EntityType}",
                    libraryId, entityType);

                return Problem(statusCode: 500, title: "Lookup Failed", detail: ex.Message);
            }
        }

        /// <summary>
        /// Retrieves all Component IDs for a given Library.
        /// </summary>
        [HttpGet("library/{libraryId:guid}/components")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetComponentIds([FromRoute] Guid libraryId)
        {
            try
            {
                var ids = _service.GetComponentIds(libraryId);
                return Ok(new { LibraryId = libraryId, Ids = ids });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving component ids for library {LibraryId}.", libraryId);
                return Problem(statusCode: 500, title: "Lookup Failed", detail: ex.Message);
            }
        }

        /// <summary>
        /// Retrieves all Threat IDs for a given Library.
        /// </summary>
        [HttpGet("library/{libraryId:guid}/threats")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetThreatIds([FromRoute] Guid libraryId)
        {
            try
            {
                var ids = _service.GetThreatIds(libraryId);
                return Ok(new { LibraryId = libraryId, Ids = ids });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving threat ids for library {LibraryId}.", libraryId);
                return Problem(statusCode: 500, title: "Lookup Failed", detail: ex.Message);
            }
        }

        /// <summary>
        /// Retrieves all Security Requirement IDs for a given Library.
        /// </summary>
        [HttpGet("library/{libraryId:guid}/security-requirements")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetSecurityRequirementIds([FromRoute] Guid libraryId)
        {
            try
            {
                var ids = _service.GetSecurityRequirementIds(libraryId);
                return Ok(new { LibraryId = libraryId, Ids = ids });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving security requirement ids for library {LibraryId}.", libraryId);
                return Problem(statusCode: 500, title: "Lookup Failed", detail: ex.Message);
            }
        }

        /// <summary>
        /// Retrieves all Property IDs for a given Library.
        /// </summary>
        [HttpGet("library/{libraryId:guid}/properties")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetPropertyIds([FromRoute] Guid libraryId)
        {
            try
            {
                var ids = _service.GetPropertyIds(libraryId);
                return Ok(new { LibraryId = libraryId, Ids = ids });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving property ids for library {LibraryId}.", libraryId);
                return Problem(statusCode: 500, title: "Lookup Failed", detail: ex.Message);
            }
        }

        /// <summary>
        /// Retrieves all Test Case IDs for a given Library.
        /// </summary>
        [HttpGet("library/{libraryId:guid}/test-cases")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetTestCaseIds([FromRoute] Guid libraryId)
        {
            try
            {
                var ids = _service.GetTestCaseIds(libraryId);
                return Ok(new { LibraryId = libraryId, Ids = ids });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving test case ids for library {LibraryId}.", libraryId);
                return Problem(statusCode: 500, title: "Lookup Failed", detail: ex.Message);
            }
        }
    }
}