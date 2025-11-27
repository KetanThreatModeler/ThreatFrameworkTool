  using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
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
        public GuidIndexController(IGuidIndexService service, IOptions<PathOptions> options) {
            _service = service;
            _pathOptions = options.Value ?? throw new ArgumentNullException(nameof(options));
        } 

        [HttpPost("generate")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GenerateAsync(CancellationToken ct)
        {
            string path = _pathOptions.IndexYaml;
            if (string.IsNullOrWhiteSpace(path))
                return Problem(statusCode: 400, title: "Bad request", detail: "Path is required.");
            try
            {
                await _service.GenerateAsync(path);
                return Ok(new { message = "Index generated.", path });
            }
            catch (Exception ex)
            {
                throw ex;
                return Problem(statusCode: 400, title: "Generation failed", detail: ex.Message);
            }
        }

        [HttpPost("refresh")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RefreshAsync()
        {
            string path = _pathOptions.IndexYaml;
            if (string.IsNullOrWhiteSpace(path))
                return Problem(statusCode: 400, title: "Bad request", detail: "Path is required.");
            try
            {
                await _service.RefreshAsync(path);
                return Ok(new { message = "Cache refreshed.", path });
            }
            catch (Exception ex)
            {
                return Problem(statusCode: 400, title: "Refresh failed", detail: ex.Message);
            }
        }


        // 3) Get int id for a GUID (uses in-memory; if cache empty, loads from file)
        [HttpGet("{guid:guid}")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAsync([FromRoute] Guid guid)
        {
            try
            {
                var id =  _service.GetInt(guid);
                return Ok(new { Guid = guid, Id = id });
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
    }
}
