using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ContentDomainService.Data;
using Fabrica.Domain.Esb.Models;

namespace ContentDomainService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EsbController : ControllerBase
{
    private readonly ContentDbContext _context;
    private readonly ILogger<EsbController> _logger;

    public EsbController(ContentDbContext context, ILogger<EsbController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/esb/outbox
    [HttpGet("outbox")]
    public async Task<ActionResult<IEnumerable<OutboxEvent>>> GetOutboxEvents(
        [FromQuery] string? status = "pending",
        [FromQuery] int limit = 100)
    {
        try
        {
            var query = _context.OutboxEvents.AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(e => e.Status == status);
            }

            var events = await query
                .OrderBy(e => e.CreatedAt)
                .Take(limit)
                .ToListAsync();

            return Ok(events);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting outbox events");
            return BadRequest(new { error = ex.Message });
        }
    }

    // PUT: api/esb/outbox/{id}/status
    [HttpPut("outbox/{id}/status")]
    public async Task<IActionResult> UpdateOutboxEventStatus(Guid id, [FromBody] UpdateStatusRequest request)
    {
        try
        {
            var outboxEvent = await _context.OutboxEvents.FindAsync(id);

            if (outboxEvent == null)
            {
                return NotFound(new { error = "Outbox event not found" });
            }

            outboxEvent.Status = request.Status;

            if (request.Status == "processed")
            {
                outboxEvent.ProcessedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return Ok(outboxEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating outbox event status {EventId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    // GET: api/esb/outbox-config
    [HttpGet("outbox-config")]
    public async Task<ActionResult<IEnumerable<OutboxConfig>>> GetOutboxConfigs()
    {
        try
        {
            var configs = await _context.OutboxConfigs.ToListAsync();
            return Ok(configs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting outbox configs");
            return BadRequest(new { error = ex.Message });
        }
    }

    // PUT: api/esb/outbox-config/{id}
    [HttpPut("outbox-config/{id}")]
    public async Task<IActionResult> UpdateOutboxConfig(Guid id, [FromBody] UpdateOutboxConfigRequest request)
    {
        try
        {
            var config = await _context.OutboxConfigs.FindAsync(id);

            if (config == null)
            {
                return NotFound(new { error = "Outbox config not found" });
            }

            if (request.CaptureInsert.HasValue) config.CaptureInsert = request.CaptureInsert.Value;
            if (request.CaptureUpdate.HasValue) config.CaptureUpdate = request.CaptureUpdate.Value;
            if (request.CaptureDelete.HasValue) config.CaptureDelete = request.CaptureDelete.Value;
            if (request.IsActive.HasValue) config.IsActive = request.IsActive.Value;
            if (request.TopicName != null) config.TopicName = request.TopicName;

            await _context.SaveChangesAsync();

            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating outbox config {ConfigId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    // GET: api/esb/cache-config
    [HttpGet("cache-config")]
    public async Task<ActionResult<IEnumerable<CacheConfig>>> GetCacheConfigs()
    {
        try
        {
            var configs = await _context.CacheConfigs.ToListAsync();
            return Ok(configs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache configs");
            return BadRequest(new { error = ex.Message });
        }
    }

    // PUT: api/esb/cache-config/{id}
    [HttpPut("cache-config/{id}")]
    public async Task<IActionResult> UpdateCacheConfig(Guid id, [FromBody] UpdateCacheConfigRequest request)
    {
        try
        {
            var config = await _context.CacheConfigs.FindAsync(id);

            if (config == null)
            {
                return NotFound(new { error = "Cache config not found" });
            }

            if (request.ListenCreate.HasValue) config.ListenCreate = request.ListenCreate.Value;
            if (request.ListenUpdate.HasValue) config.ListenUpdate = request.ListenUpdate.Value;
            if (request.ListenDelete.HasValue) config.ListenDelete = request.ListenDelete.Value;
            if (request.IsActive.HasValue) config.IsActive = request.IsActive.Value;
            if (request.ConsumerGroup != null) config.ConsumerGroup = request.ConsumerGroup;
            if (request.CacheTtlSeconds.HasValue) config.CacheTtlSeconds = request.CacheTtlSeconds;

            await _context.SaveChangesAsync();

            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating cache config {ConfigId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    // GET: api/esb/cache
    [HttpGet("cache")]
    public async Task<ActionResult<IEnumerable<CacheEntry>>> GetCacheEntries(
        [FromQuery] string? sourceDomain = null,
        [FromQuery] string? sourceTable = null,
        [FromQuery] string? tenantId = null,
        [FromQuery] bool includeDeleted = false)
    {
        try
        {
            var query = _context.CacheEntries.AsQueryable();

            if (!string.IsNullOrEmpty(sourceDomain))
            {
                query = query.Where(c => c.SourceDomain == sourceDomain);
            }

            if (!string.IsNullOrEmpty(sourceTable))
            {
                query = query.Where(c => c.SourceTable == sourceTable);
            }

            if (!string.IsNullOrEmpty(tenantId))
            {
                query = query.Where(c => c.TenantId == tenantId);
            }

            if (!includeDeleted)
            {
                query = query.Where(c => !c.IsDeleted);
            }

            var entries = await query
                .OrderByDescending(c => c.UpdatedAt)
                .Take(100)
                .ToListAsync();

            return Ok(entries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache entries");
            return BadRequest(new { error = ex.Message });
        }
    }
}

// Request DTOs
public class UpdateStatusRequest
{
    public string Status { get; set; } = string.Empty;
}

public class UpdateOutboxConfigRequest
{
    public bool? CaptureInsert { get; set; }
    public bool? CaptureUpdate { get; set; }
    public bool? CaptureDelete { get; set; }
    public bool? IsActive { get; set; }
    public string? TopicName { get; set; }
}

public class UpdateCacheConfigRequest
{
    public bool? ListenCreate { get; set; }
    public bool? ListenUpdate { get; set; }
    public bool? ListenDelete { get; set; }
    public bool? IsActive { get; set; }
    public string? ConsumerGroup { get; set; }
    public int? CacheTtlSeconds { get; set; }
}
