using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using ThreatFramework.Infra.Contract.Index;

namespace ThreatFramework.API.Controllers
{
    [ApiController]
    [Route("api/guid-index")]
    [Produces("application/json")]
    public sealed class GuidIndexController : ControllerBase
    {
        private readonly IGuidIndexService _service;

        public GuidIndexController(IGuidIndexService service) => _service = service;

        // 1) Generate index.yaml (uses IGuidSource internally)
        [HttpPost("generate")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GenerateAsync([FromBody] GenerateRequest request, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request?.Path))
                return Problem(statusCode: 400, title: "Bad request", detail: "Path is required.");
            try
            {
                await _service.GenerateAsync(request.Path!);
                return Ok(new { message = "Index generated.", path = request.Path });
            }
            catch (Exception ex)
            {
                return Problem(statusCode: 400, title: "Generation failed", detail: ex.Message);
            }
        }

        // 2) Refresh in-memory cache from file (uses GuidIndexRepository)
        [HttpPost("refresh")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RefreshAsync([FromBody] RefreshRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Path))
                return Problem(statusCode: 400, title: "Bad request", detail: "Path is required.");
            try
            {
                await _service.RefreshAsync(request.Path!);
                return Ok(new { message = "Cache refreshed.", path = request.Path });
            }
            catch (Exception ex)
            {
                return Problem(statusCode: 400, title: "Refresh failed", detail: ex.Message);
            }
        }

        // 3) Get int id for a GUID (uses in-memory; if cache empty, loads from file)
        [HttpGet("{guid:guid}")]
        [ProducesResponseType(typeof(GetIntResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAsync([FromRoute] Guid guid)
        {
            try
            {
                var id =  _service.GetInt(guid);
                return Ok(new GetIntResponse { Guid = guid, Id = id });
            }
            catch (KeyNotFoundException)
            {
                return Problem(statusCode: 404, title: "Not found", detail: $"GUID '{guid}' not found in index.");
            }
            catch (Exception ex)
            {
                return Problem(statusCode: 400, title: "Lookup failed", detail: ex.Message);
            }
        }

        // DTOs (controller-only)
        public sealed class GenerateRequest { [Required] public string? Path { get; init; } }
        public sealed class RefreshRequest { [Required] public string? Path { get; init; } }
        public sealed class GetIntResponse { public Guid Guid { get; init; } public int Id { get; init; } }
    }
}
