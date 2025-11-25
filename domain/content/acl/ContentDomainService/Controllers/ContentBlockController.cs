using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ContentDomainService.Data;
using ContentDomainService.Models;
using System.Text.Json;

namespace ContentDomainService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContentBlockController : ControllerBase
{
    private readonly ContentDbContext _context;
    private readonly ILogger<ContentBlockController> _logger;

    public ContentBlockController(ContentDbContext context, ILogger<ContentBlockController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/contentblock
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetContentBlocks(
        [FromQuery] string tenantId = "tenant-test",
        [FromQuery] string? localeCode = "en-US",
        [FromQuery] string? blockType = null,
        [FromQuery] bool? isGlobal = null)
    {
        try
        {
            var query = _context.ContentBlocks
                .Include(b => b.Translations)
                .Where(b => b.TenantId == tenantId && b.IsActive);

            if (!string.IsNullOrEmpty(blockType))
            {
                query = query.Where(b => b.BlockType == blockType);
            }

            if (isGlobal.HasValue)
            {
                query = query.Where(b => b.IsGlobal == isGlobal.Value);
            }

            var rawBlocks = await query
                .OrderBy(b => b.Code)
                .Select(b => new
                {
                    b.Id,
                    b.TenantId,
                    b.Code,
                    b.BlockType,
                    b.Settings,
                    b.IsGlobal,
                    b.IsActive,
                    b.CreatedAt,
                    b.UpdatedAt,
                    Translation = b.Translations
                        .Where(t => t.LocaleCode == localeCode)
                        .Select(t => new
                        {
                            t.Title,
                            t.Subtitle,
                            t.Body,
                            t.CtaText,
                            t.CtaUrl,
                            t.AdditionalData
                        })
                        .FirstOrDefault()
                })
                .ToListAsync();

            // Parse JSON after query execution
            var blocks = rawBlocks.Select(b => new
            {
                b.Id,
                b.TenantId,
                b.Code,
                b.BlockType,
                Settings = !string.IsNullOrEmpty(b.Settings) ? JsonDocument.Parse(b.Settings).RootElement : (JsonElement?)null,
                b.IsGlobal,
                b.IsActive,
                b.CreatedAt,
                b.UpdatedAt,
                Translation = b.Translation == null ? null : new
                {
                    b.Translation.Title,
                    b.Translation.Subtitle,
                    b.Translation.Body,
                    b.Translation.CtaText,
                    b.Translation.CtaUrl,
                    AdditionalData = !string.IsNullOrEmpty(b.Translation.AdditionalData) ? JsonDocument.Parse(b.Translation.AdditionalData).RootElement : (JsonElement?)null
                }
            });

            return Ok(blocks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting content blocks");
            return BadRequest(new { error = ex.Message });
        }
    }

    // GET: api/contentblock/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetContentBlock(Guid id, [FromQuery] string? localeCode = null)
    {
        try
        {
            var block = await _context.ContentBlocks
                .Include(b => b.Translations)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (block == null)
            {
                return NotFound(new { error = "Content block not found" });
            }

            var translations = localeCode != null
                ? block.Translations.Where(t => t.LocaleCode == localeCode).ToList()
                : block.Translations.ToList();

            return Ok(new
            {
                block.Id,
                block.TenantId,
                block.Code,
                block.BlockType,
                Settings = block.Settings != null ? JsonDocument.Parse(block.Settings).RootElement : (JsonElement?)null,
                block.IsGlobal,
                block.IsActive,
                block.CreatedAt,
                block.UpdatedAt,
                Translations = translations.Select(t => new
                {
                    t.Id,
                    t.LocaleCode,
                    t.Title,
                    t.Subtitle,
                    t.Body,
                    t.CtaText,
                    t.CtaUrl,
                    AdditionalData = t.AdditionalData != null ? JsonDocument.Parse(t.AdditionalData).RootElement : (JsonElement?)null
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting content block {BlockId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    // GET: api/contentblock/code/{code}
    [HttpGet("code/{code}")]
    public async Task<ActionResult<object>> GetContentBlockByCode(
        string code,
        [FromQuery] string tenantId = "tenant-test",
        [FromQuery] string? localeCode = "en-US")
    {
        try
        {
            var block = await _context.ContentBlocks
                .Include(b => b.Translations)
                .FirstOrDefaultAsync(b => b.TenantId == tenantId && b.Code == code);

            if (block == null)
            {
                return NotFound(new { error = "Content block not found" });
            }

            var translation = block.Translations
                .FirstOrDefault(t => t.LocaleCode == localeCode)
                ?? block.Translations.FirstOrDefault();

            // Build response in format expected by ContentBlock component
            return Ok(new
            {
                block.Id,
                block.Code,
                block.BlockType,
                Settings = block.Settings != null ? JsonDocument.Parse(block.Settings).RootElement : (JsonElement?)null,
                // Content for ContentBlock component
                Title = translation?.Title,
                Subtitle = translation?.Subtitle,
                Body = translation?.Body,
                CtaText = translation?.CtaText,
                CtaUrl = translation?.CtaUrl,
                AdditionalData = translation?.AdditionalData != null
                    ? JsonDocument.Parse(translation.AdditionalData).RootElement
                    : (JsonElement?)null,
                LocaleCode = translation?.LocaleCode
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting content block by code {Code}", code);
            return BadRequest(new { error = ex.Message });
        }
    }

    // POST: api/contentblock
    [HttpPost]
    public async Task<ActionResult<ContentBlock>> CreateContentBlock([FromBody] CreateContentBlockRequest request)
    {
        try
        {
            var block = new ContentBlock
            {
                TenantId = request.TenantId,
                Code = request.Code,
                BlockType = request.BlockType,
                Settings = request.Settings != null ? JsonSerializer.Serialize(request.Settings) : null,
                IsGlobal = request.IsGlobal,
                IsActive = request.IsActive
            };

            _context.ContentBlocks.Add(block);

            // Add translations
            if (request.Translations != null)
            {
                foreach (var translationRequest in request.Translations)
                {
                    var translation = new ContentBlockTranslation
                    {
                        ContentBlockId = block.Id,
                        LocaleCode = translationRequest.LocaleCode,
                        Title = translationRequest.Title,
                        Subtitle = translationRequest.Subtitle,
                        Body = translationRequest.Body,
                        CtaText = translationRequest.CtaText,
                        CtaUrl = translationRequest.CtaUrl,
                        AdditionalData = translationRequest.AdditionalData != null
                            ? JsonSerializer.Serialize(translationRequest.AdditionalData)
                            : null
                    };
                    block.Translations.Add(translation);
                }
            }

            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetContentBlock), new { id = block.Id }, block);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating content block");
            return BadRequest(new { error = ex.Message });
        }
    }

    // PUT: api/contentblock/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateContentBlock(Guid id, [FromBody] UpdateContentBlockRequest request)
    {
        try
        {
            var block = await _context.ContentBlocks.FindAsync(id);

            if (block == null)
            {
                return NotFound(new { error = "Content block not found" });
            }

            if (request.Code != null) block.Code = request.Code;
            if (request.BlockType != null) block.BlockType = request.BlockType;
            if (request.Settings != null) block.Settings = JsonSerializer.Serialize(request.Settings);
            if (request.IsGlobal.HasValue) block.IsGlobal = request.IsGlobal.Value;
            if (request.IsActive.HasValue) block.IsActive = request.IsActive.Value;

            await _context.SaveChangesAsync();

            return Ok(block);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating content block {BlockId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    // DELETE: api/contentblock/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteContentBlock(Guid id)
    {
        try
        {
            var block = await _context.ContentBlocks.FindAsync(id);

            if (block == null)
            {
                return NotFound(new { error = "Content block not found" });
            }

            _context.ContentBlocks.Remove(block);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting content block {BlockId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }
}

// Request DTOs
public class CreateContentBlockRequest
{
    public string TenantId { get; set; } = "tenant-test";
    public string Code { get; set; } = string.Empty;
    public string BlockType { get; set; } = string.Empty;
    public object? Settings { get; set; }
    public bool IsGlobal { get; set; }
    public bool IsActive { get; set; } = true;
    public List<CreateBlockTranslationRequest>? Translations { get; set; }
}

public class CreateBlockTranslationRequest
{
    public string LocaleCode { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Subtitle { get; set; }
    public string? Body { get; set; }
    public string? CtaText { get; set; }
    public string? CtaUrl { get; set; }
    public object? AdditionalData { get; set; }
}

public class UpdateContentBlockRequest
{
    public string? Code { get; set; }
    public string? BlockType { get; set; }
    public object? Settings { get; set; }
    public bool? IsGlobal { get; set; }
    public bool? IsActive { get; set; }
}
