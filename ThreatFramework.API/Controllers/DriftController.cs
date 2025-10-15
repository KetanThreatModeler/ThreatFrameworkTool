using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using ThreatFramework.Drift.Contract;
using ThreatFramework.Infra.Contract.DataInsertion;

namespace ThreatFramework.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DriftController : ControllerBase
    {
        private readonly ILibraryDriftAggregator _driftAggregator;
        private readonly IDriftApplier _driftApplier;

        public DriftController(ILibraryDriftAggregator driftAggregator, IDriftApplier driftApplier)
        {
            _driftAggregator = driftAggregator;
            _driftApplier = driftApplier;
        }

        [HttpGet]
        public async Task<IActionResult> GetDrift()
        {
            try
            {
                Console.WriteLine("Getting framework drift");

                var aggregationStopwatch = Stopwatch.StartNew();
                var drift = await _driftAggregator.Drift();
                aggregationStopwatch.Stop();
                Console.WriteLine($"Drift aggregation completed in {aggregationStopwatch.ElapsedMilliseconds} ms");

                /*var applyStopwatch = Stopwatch.StartNew();
                await _driftApplier.ApplyAsync(drift);
                applyStopwatch.Stop();
                Console.WriteLine($"Drift application completed in {applyStopwatch.ElapsedMilliseconds} ms");*/

                return Ok(drift);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred while getting framework drift: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }


        [HttpPost]
        public async Task<IActionResult> ApplyDrift()
        {
            try
            {
                Console.WriteLine("Getting framework drift");

                var aggregationStopwatch = Stopwatch.StartNew();
                var drift = await _driftAggregator.Drift();
                aggregationStopwatch.Stop();
                Console.WriteLine($"Drift aggregation completed in {aggregationStopwatch.ElapsedMilliseconds} ms");

                var applyStopwatch = Stopwatch.StartNew();
                await _driftApplier.ApplyAsync(drift);
                applyStopwatch.Stop();
                Console.WriteLine($"Drift application completed in {applyStopwatch.ElapsedMilliseconds} ms");

                return Ok(drift);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred while getting framework drift: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
