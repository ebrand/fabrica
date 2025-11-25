using AdminDomainService.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Fabrica.Domain.Esb.Models;

namespace AdminDomainService.Controllers;

[ApiController]
[Route("api/esb")]
public class EsbController : ControllerBase
{
    private readonly AdminDbContext _context;
    private readonly ILogger<EsbController> _logger;

    public EsbController(AdminDbContext context, ILogger<EsbController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ==================== Database Tables ====================

    // GET: api/esb/tables
    // Returns all tables in the fabrica schema for this domain
    [HttpGet("tables")]
    public async Task<ActionResult<IEnumerable<TableInfoDto>>> GetTables()
    {
        try
        {
            var tables = await _context.Database
                .SqlQueryRaw<TableInfoDto>(@"
                    SELECT
                        table_schema as SchemaName,
                        table_name as TableName,
                        (SELECT obj_description((table_schema || '.' || table_name)::regclass, 'pg_class')) as Description
                    FROM information_schema.tables
                    WHERE table_schema = 'fabrica'
                    AND table_type = 'BASE TABLE'
                    ORDER BY table_name")
                .ToListAsync();

            return Ok(tables);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching tables");
            return BadRequest(new { error = ex.Message });
        }
    }

    // ==================== Domain Registry ====================

    // GET: api/esb/domain
    [HttpGet("domain")]
    public async Task<ActionResult<IEnumerable<EsbDomain>>> GetDomains()
    {
        try
        {
            var domains = await _context.EsbDomains
                .Where(d => d.IsActive)
                .OrderBy(d => d.DomainName)
                .ToListAsync();

            return Ok(domains);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching ESB domains");
            return BadRequest(new { error = ex.Message });
        }
    }

    // GET: api/esb/domain/{domainName}
    [HttpGet("domain/{domainName}")]
    public async Task<ActionResult<EsbDomain>> GetDomain(string domainName)
    {
        try
        {
            var domain = await _context.EsbDomains
                .FirstOrDefaultAsync(d => d.DomainName == domainName);

            if (domain == null)
            {
                return NotFound(new { error = $"Domain '{domainName}' not found" });
            }

            return Ok(domain);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching domain {DomainName}", domainName);
            return BadRequest(new { error = ex.Message });
        }
    }

    // ==================== Outbox Config (Publishing) ====================
    // Note: Admin service can only manage admin domain's outbox config directly
    // Other domains must implement their own endpoints

    // GET: api/esb/outbox-config
    [HttpGet("outbox-config")]
    public async Task<ActionResult<IEnumerable<OutboxConfig>>> GetOutboxConfigs()
    {
        try
        {
            var configs = await _context.OutboxConfigs
                .OrderBy(c => c.SchemaName)
                .ThenBy(c => c.TableName)
                .ToListAsync();

            return Ok(configs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching outbox configs");
            return BadRequest(new { error = ex.Message });
        }
    }

    // GET: api/esb/outbox-config/{id}
    [HttpGet("outbox-config/{id}")]
    public async Task<ActionResult<OutboxConfig>> GetOutboxConfig(Guid id)
    {
        try
        {
            var config = await _context.OutboxConfigs.FindAsync(id);

            if (config == null)
            {
                return NotFound(new { error = "Outbox config not found" });
            }

            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching outbox config {Id}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    // POST: api/esb/outbox-config
    [HttpPost("outbox-config")]
    public async Task<ActionResult<OutboxConfig>> CreateOutboxConfig(CreateOutboxConfigDto dto)
    {
        try
        {
            // Check if config already exists for this schema/table
            var existing = await _context.OutboxConfigs
                .FirstOrDefaultAsync(c => c.SchemaName == dto.SchemaName && c.TableName == dto.TableName);

            if (existing != null)
            {
                return Conflict(new { error = $"Config already exists for {dto.SchemaName}.{dto.TableName}" });
            }

            const string domainName = "admin";
            var topicName = $"{domainName}.{dto.TableName}";

            var config = new OutboxConfig
            {
                SchemaName = dto.SchemaName,
                TableName = dto.TableName,
                DomainName = domainName,
                TopicName = topicName,
                CaptureInsert = dto.CaptureInsert,
                CaptureUpdate = dto.CaptureUpdate,
                CaptureDelete = dto.CaptureDelete,
                IsActive = dto.IsActive,
                Description = dto.Description,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.OutboxConfigs.Add(config);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetOutboxConfig), new { id = config.Id }, config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating outbox config");
            return BadRequest(new { error = ex.Message });
        }
    }

    // PUT: api/esb/outbox-config/{id}
    [HttpPut("outbox-config/{id}")]
    public async Task<ActionResult<OutboxConfig>> UpdateOutboxConfig(Guid id, UpdateOutboxConfigDto dto)
    {
        try
        {
            var config = await _context.OutboxConfigs.FindAsync(id);

            if (config == null)
            {
                return NotFound(new { error = "Outbox config not found" });
            }

            if (dto.CaptureInsert.HasValue) config.CaptureInsert = dto.CaptureInsert.Value;
            if (dto.CaptureUpdate.HasValue) config.CaptureUpdate = dto.CaptureUpdate.Value;
            if (dto.CaptureDelete.HasValue) config.CaptureDelete = dto.CaptureDelete.Value;
            if (dto.IsActive.HasValue) config.IsActive = dto.IsActive.Value;
            if (dto.Description != null) config.Description = dto.Description;

            config.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating outbox config {Id}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    // DELETE: api/esb/outbox-config/{id}
    [HttpDelete("outbox-config/{id}")]
    public async Task<IActionResult> DeleteOutboxConfig(Guid id)
    {
        try
        {
            var config = await _context.OutboxConfigs.FindAsync(id);

            if (config == null)
            {
                return NotFound(new { error = "Outbox config not found" });
            }

            _context.OutboxConfigs.Remove(config);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Outbox config deleted successfully", id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting outbox config {Id}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    // ==================== Cache Config (Consuming) ====================

    // GET: api/esb/cache-config
    [HttpGet("cache-config")]
    public async Task<ActionResult<IEnumerable<CacheConfig>>> GetCacheConfigs()
    {
        try
        {
            var configs = await _context.CacheConfigs
                .OrderBy(c => c.SourceDomain)
                .ThenBy(c => c.SourceTable)
                .ToListAsync();

            return Ok(configs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching cache configs");
            return BadRequest(new { error = ex.Message });
        }
    }

    // GET: api/esb/cache-config/{id}
    [HttpGet("cache-config/{id}")]
    public async Task<ActionResult<CacheConfig>> GetCacheConfig(Guid id)
    {
        try
        {
            var config = await _context.CacheConfigs.FindAsync(id);

            if (config == null)
            {
                return NotFound(new { error = "Cache config not found" });
            }

            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching cache config {Id}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    // POST: api/esb/cache-config
    [HttpPost("cache-config")]
    public async Task<ActionResult<CacheConfig>> CreateCacheConfig(CreateCacheConfigDto dto)
    {
        try
        {
            // Check if config already exists for this source
            var existing = await _context.CacheConfigs
                .FirstOrDefaultAsync(c =>
                    c.SourceDomain == dto.SourceDomain &&
                    c.SourceSchema == dto.SourceSchema &&
                    c.SourceTable == dto.SourceTable);

            if (existing != null)
            {
                return Conflict(new { error = $"Config already exists for {dto.SourceDomain}.{dto.SourceSchema}.{dto.SourceTable}" });
            }

            // Generate consumer group name if not provided
            var consumerGroup = dto.ConsumerGroup ?? $"admin-{dto.SourceDomain}.{dto.SourceTable}";

            var config = new CacheConfig
            {
                SourceDomain = dto.SourceDomain,
                SourceSchema = dto.SourceSchema,
                SourceTable = dto.SourceTable,
                ConsumerGroup = consumerGroup,
                ListenCreate = dto.ListenCreate,
                ListenUpdate = dto.ListenUpdate,
                ListenDelete = dto.ListenDelete,
                IsActive = dto.IsActive,
                CacheTtlSeconds = dto.CacheTtlSeconds,
                Description = dto.Description,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.CacheConfigs.Add(config);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCacheConfig), new { id = config.Id }, config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating cache config");
            return BadRequest(new { error = ex.Message });
        }
    }

    // PUT: api/esb/cache-config/{id}
    [HttpPut("cache-config/{id}")]
    public async Task<ActionResult<CacheConfig>> UpdateCacheConfig(Guid id, UpdateCacheConfigDto dto)
    {
        try
        {
            var config = await _context.CacheConfigs.FindAsync(id);

            if (config == null)
            {
                return NotFound(new { error = "Cache config not found" });
            }

            if (dto.ListenCreate.HasValue) config.ListenCreate = dto.ListenCreate.Value;
            if (dto.ListenUpdate.HasValue) config.ListenUpdate = dto.ListenUpdate.Value;
            if (dto.ListenDelete.HasValue) config.ListenDelete = dto.ListenDelete.Value;
            if (dto.IsActive.HasValue) config.IsActive = dto.IsActive.Value;
            if (dto.CacheTtlSeconds.HasValue) config.CacheTtlSeconds = dto.CacheTtlSeconds;
            if (dto.Description != null) config.Description = dto.Description;

            config.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating cache config {Id}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    // DELETE: api/esb/cache-config/{id}
    [HttpDelete("cache-config/{id}")]
    public async Task<IActionResult> DeleteCacheConfig(Guid id)
    {
        try
        {
            var config = await _context.CacheConfigs.FindAsync(id);

            if (config == null)
            {
                return NotFound(new { error = "Cache config not found" });
            }

            _context.CacheConfigs.Remove(config);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Cache config deleted successfully", id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting cache config {Id}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    // ==================== Combined ESB Summary ====================

    // GET: api/esb/summary
    // Returns a comprehensive view of what admin domain publishes and consumes
    [HttpGet("summary")]
    public async Task<ActionResult<EsbSummaryDto>> GetEsbSummary()
    {
        try
        {
            var domains = await _context.EsbDomains.Where(d => d.IsActive).ToListAsync();
            var outboxConfigs = await _context.OutboxConfigs.Where(c => c.IsActive).ToListAsync();
            var cacheConfigs = await _context.CacheConfigs.Where(c => c.IsActive).ToListAsync();

            var summary = new EsbSummaryDto
            {
                CurrentDomain = "admin",
                Domains = domains.Select(d => new DomainSummaryDto
                {
                    DomainName = d.DomainName,
                    DisplayName = d.DisplayName,
                    ServiceUrl = d.ServiceUrl,
                    PublishesEvents = d.PublishesEvents,
                    ConsumesEvents = d.ConsumesEvents
                }).ToList(),
                Publishing = outboxConfigs.Select(c => new PublishConfigDto
                {
                    Id = c.Id,
                    SchemaName = c.SchemaName,
                    TableName = c.TableName,
                    CaptureInsert = c.CaptureInsert,
                    CaptureUpdate = c.CaptureUpdate,
                    CaptureDelete = c.CaptureDelete,
                    Topics = GetTopicsForConfig(c)
                }).ToList(),
                Consuming = cacheConfigs.Select(c => new ConsumeConfigDto
                {
                    Id = c.Id,
                    SourceDomain = c.SourceDomain,
                    SourceSchema = c.SourceSchema,
                    SourceTable = c.SourceTable,
                    ListenCreate = c.ListenCreate,
                    ListenUpdate = c.ListenUpdate,
                    ListenDelete = c.ListenDelete,
                    CacheTtlSeconds = c.CacheTtlSeconds,
                    Topics = GetTopicsForCacheConfig(c)
                }).ToList()
            };

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching ESB summary");
            return BadRequest(new { error = ex.Message });
        }
    }

    private static List<string> GetTopicsForConfig(OutboxConfig config)
    {
        var topics = new List<string>();
        if (config.CaptureInsert) topics.Add($"{config.TableName}.created");
        if (config.CaptureUpdate) topics.Add($"{config.TableName}.updated");
        if (config.CaptureDelete) topics.Add($"{config.TableName}.deleted");
        return topics;
    }

    private static List<string> GetTopicsForCacheConfig(CacheConfig config)
    {
        var topics = new List<string>();
        if (config.ListenCreate) topics.Add($"{config.SourceTable}.created");
        if (config.ListenUpdate) topics.Add($"{config.SourceTable}.updated");
        if (config.ListenDelete) topics.Add($"{config.SourceTable}.deleted");
        return topics;
    }
}

// ==================== DTOs ====================

public class CreateOutboxConfigDto
{
    public string SchemaName { get; set; } = "fabrica";
    public string TableName { get; set; } = string.Empty;
    public bool CaptureInsert { get; set; } = true;
    public bool CaptureUpdate { get; set; } = true;
    public bool CaptureDelete { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
}

public class UpdateOutboxConfigDto
{
    public bool? CaptureInsert { get; set; }
    public bool? CaptureUpdate { get; set; }
    public bool? CaptureDelete { get; set; }
    public bool? IsActive { get; set; }
    public string? Description { get; set; }
}

public class CreateCacheConfigDto
{
    public string SourceDomain { get; set; } = string.Empty;
    public string SourceSchema { get; set; } = "fabrica";
    public string SourceTable { get; set; } = string.Empty;
    public string? ConsumerGroup { get; set; }
    public bool ListenCreate { get; set; } = true;
    public bool ListenUpdate { get; set; } = true;
    public bool ListenDelete { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public int? CacheTtlSeconds { get; set; }
    public string? Description { get; set; }
}

public class UpdateCacheConfigDto
{
    public bool? ListenCreate { get; set; }
    public bool? ListenUpdate { get; set; }
    public bool? ListenDelete { get; set; }
    public bool? IsActive { get; set; }
    public int? CacheTtlSeconds { get; set; }
    public string? Description { get; set; }
}

public class EsbSummaryDto
{
    public string CurrentDomain { get; set; } = string.Empty;
    public List<DomainSummaryDto> Domains { get; set; } = new();
    public List<PublishConfigDto> Publishing { get; set; } = new();
    public List<ConsumeConfigDto> Consuming { get; set; } = new();
}

public class DomainSummaryDto
{
    public string DomainName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? ServiceUrl { get; set; }
    public bool PublishesEvents { get; set; }
    public bool ConsumesEvents { get; set; }
}

public class PublishConfigDto
{
    public Guid Id { get; set; }
    public string SchemaName { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public bool CaptureInsert { get; set; }
    public bool CaptureUpdate { get; set; }
    public bool CaptureDelete { get; set; }
    public List<string> Topics { get; set; } = new();
}

public class ConsumeConfigDto
{
    public Guid Id { get; set; }
    public string SourceDomain { get; set; } = string.Empty;
    public string SourceSchema { get; set; } = string.Empty;
    public string SourceTable { get; set; } = string.Empty;
    public bool ListenCreate { get; set; }
    public bool ListenUpdate { get; set; }
    public bool ListenDelete { get; set; }
    public int? CacheTtlSeconds { get; set; }
    public List<string> Topics { get; set; } = new();
}

public class TableInfoDto
{
    public string SchemaName { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string? Description { get; set; }
}
