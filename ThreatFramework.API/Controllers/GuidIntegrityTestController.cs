using Microsoft.AspNetCore.Mvc;
using ThreatFramework.Infra.Contract.DataInsertion;
using ThreatFramework.Infra.Contract.DataInsertion.Dto;

namespace ThreatFramework.API.Controllers
{
    [ApiController]
    [Route("api/guid-integrity")]
    public sealed class GuidIntegrityTestController : ControllerBase
    {
        private readonly IGuidIntegrityService _service;

        public GuidIntegrityTestController(IGuidIntegrityService service)
        {
            _service = service;
        }

        // GET api/guid-integrity/test-missing
        // Hardcoded GUIDs purely for test/demo. Adjust as you like.
        [HttpGet("test-missing")]
        public async Task<ActionResult<MissingGuidsByEntity>> GetMissingGuidsForTest(CancellationToken ct)
        {
            var request = new CheckMissingGuidsRequest
            {

                ThreatIds = new[]
                {
                    Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                },
                SecurityRequirementIds = new[]
                {
                    Guid.Parse("44444444-4444-4444-4444-444444444444"),
                    Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
                },
                PropertyIds = new[]
                {
                    Guid.Parse("55555555-5555-5555-5555-555555555555"),
                    Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
                }
            };

            var result = await _service.GetMissingGuidsAsync(request);
            return Ok(result);
        }
    }
}
