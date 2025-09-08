using Microsoft.AspNetCore.Mvc;
using ThreatFramework.Core.IndexModel;
using ThreatFramework.Infra.Contract.Index;

namespace ThreatFramework.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class IndexController : ControllerBase
    {
        private readonly IIndexService _indexService;
        private readonly ILogger<IndexController> _logger;

        public IndexController(IIndexService indexService, ILogger<IndexController> logger)
        {
            _indexService = indexService ?? throw new ArgumentNullException(nameof(indexService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Refreshes the in-memory index cache from the YAML file
        /// </summary>
        /// <returns>Result of the refresh operation</returns>
        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshAsync()
        {
            try
            {
                _logger.LogInformation("Index refresh requested");
                
                var success = await _indexService.RefreshAsync();
                
                if (success)
                {
                    var stats = _indexService.GetCacheStatistics();
                    _logger.LogInformation("Index refresh completed successfully. Total items: {TotalItems}", stats.TotalItems);
                    
                    return Ok(new
                    {
                        Success = true,
                        Message = "Index cache refreshed successfully",
                        Statistics = stats
                    });
                }
                else
                {
                    _logger.LogWarning("Index refresh failed");
                    return BadRequest(new
                    {
                        Success = false,
                        Message = "Failed to refresh index cache. Check logs for details."
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during index refresh");
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "An unexpected error occurred during refresh operation"
                });
            }
        }

        /// <summary>
        /// Gets cache statistics and health information
        /// </summary>
        /// <returns>Current cache statistics</returns>
        [HttpGet("statistics")]
        public IActionResult GetStatistics()
        {
            try
            {
                var stats = _indexService.GetCacheStatistics();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving cache statistics");
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "An error occurred while retrieving statistics"
                });
            }
        }

        /// <summary>
        /// Looks up an ID by GUID
        /// </summary>
        /// <param name="guid">The GUID to lookup</param>
        /// <returns>The corresponding ID if found</returns>
        [HttpGet("lookup/by-guid/{guid:guid}")]
        public IActionResult GetIdByGuid(Guid guid)
        {
            try
            {
                var id = _indexService.GetIdByGuid(guid);
                
                if (id.HasValue)
                {
                    return Ok(new { Guid = guid, Id = id.Value });
                }
                else
                {
                    return NotFound(new { Message = $"No item found with GUID: {guid}" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error looking up ID by GUID: {Guid}", guid);
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "An error occurred during lookup operation"
                });
            }
        }

        /// <summary>
        /// Looks up an ID by kind and GUID combination
        /// </summary>
        /// <param name="kind">The kind of the item</param>
        /// <param name="guid">The GUID to lookup</param>
        /// <returns>The corresponding ID if found</returns>
        [HttpGet("lookup/by-kind-guid/{kind}/{guid:guid}")]
        public IActionResult GetIdByKindAndGuid(string kind, Guid guid)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(kind))
                {
                    return BadRequest(new { Message = "Kind parameter cannot be empty" });
                }

                if (!Enum.TryParse<EntityKind>(kind, true, out var entityKind))
                {
                    return BadRequest(new { Message = $"Invalid kind '{kind}'. Valid values are: {string.Join(", ", Enum.GetNames<EntityKind>())}" });
                }

                var id = _indexService.GetIdByKindAndGuid(entityKind, guid);
                
                if (id.HasValue)
                {
                    return Ok(new { Kind = kind, Guid = guid, Id = id.Value });
                }
                else
                {
                    return NotFound(new { Message = $"No item found with kind '{kind}' and GUID: {guid}" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error looking up ID by kind and GUID: {Kind}, {Guid}", kind, guid);
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "An error occurred during lookup operation"
                });
            }
        }

        /// <summary>
        /// Gets full item details by GUID
        /// </summary>
        /// <param name="guid">The GUID to lookup</param>
        /// <returns>The full IndexItem if found</returns>
        [HttpGet("item/{guid:guid}")]
        public IActionResult GetItemByGuid(Guid guid)
        {
            try
            {
                var item = _indexService.GetItemByGuid(guid);
                
                if (item != null)
                {
                    return Ok(item);
                }
                else
                {
                    return NotFound(new { Message = $"No item found with GUID: {guid}" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving item by GUID: {Guid}", guid);
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "An error occurred during lookup operation"
                });
            }
        }

        /// <summary>
        /// Gets all items of a specific kind
        /// </summary>
        /// <param name="kind">The kind to filter by</param>
        /// <returns>Collection of items matching the kind</returns>
        [HttpGet("items/by-kind/{kind}")]
        public IActionResult GetItemsByKind(string kind)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(kind))
                {
                    return BadRequest(new { Message = "Kind parameter cannot be empty" });
                }

                if (!Enum.TryParse<EntityKind>(kind, true, out var entityKind))
                {
                    return BadRequest(new { Message = $"Invalid kind '{kind}'. Valid values are: {string.Join(", ", Enum.GetNames<EntityKind>())}" });
                }

                var items = _indexService.GetItemsByKind(entityKind);
                return Ok(new { Kind = kind, Items = items, Count = items.Count() });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving items by kind: {Kind}", kind);
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "An error occurred during lookup operation"
                });
            }
        }
    }
}