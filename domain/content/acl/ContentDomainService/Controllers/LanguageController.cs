using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ContentDomainService.Data;
using ContentDomainService.Models;

namespace ContentDomainService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LanguageController : ControllerBase
{
    private readonly ContentDbContext _context;
    private readonly ILogger<LanguageController> _logger;

    public LanguageController(ContentDbContext context, ILogger<LanguageController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/language
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Language>>> GetLanguages(
        [FromQuery] string tenantId = "tenant-test",
        [FromQuery] bool? activeOnly = true)
    {
        try
        {
            var query = _context.Languages
                .Where(l => l.TenantId == tenantId);

            if (activeOnly == true)
            {
                query = query.Where(l => l.IsActive);
            }

            var languages = await query
                .OrderBy(l => l.DisplayOrder)
                .ThenBy(l => l.Name)
                .ToListAsync();

            return Ok(languages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting languages");
            return BadRequest(new { error = ex.Message });
        }
    }

    // GET: api/language/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<Language>> GetLanguage(Guid id)
    {
        try
        {
            var language = await _context.Languages.FindAsync(id);

            if (language == null)
            {
                return NotFound(new { error = "Language not found" });
            }

            return Ok(language);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting language {LanguageId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    // GET: api/language/default
    [HttpGet("default")]
    public async Task<ActionResult<Language>> GetDefaultLanguage([FromQuery] string tenantId = "tenant-test")
    {
        try
        {
            var language = await _context.Languages
                .FirstOrDefaultAsync(l => l.TenantId == tenantId && l.IsDefault && l.IsActive);

            if (language == null)
            {
                // Fall back to first active language
                language = await _context.Languages
                    .FirstOrDefaultAsync(l => l.TenantId == tenantId && l.IsActive);
            }

            if (language == null)
            {
                return NotFound(new { error = "No default language configured" });
            }

            return Ok(language);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting default language");
            return BadRequest(new { error = ex.Message });
        }
    }

    // POST: api/language
    [HttpPost]
    public async Task<ActionResult<Language>> CreateLanguage([FromBody] CreateLanguageRequest request)
    {
        try
        {
            // Check for existing locale code
            var existing = await _context.Languages
                .AnyAsync(l => l.TenantId == request.TenantId && l.LocaleCode == request.LocaleCode);

            if (existing)
            {
                return BadRequest(new { error = "Language with this locale code already exists" });
            }

            var language = new Language
            {
                TenantId = request.TenantId,
                LocaleCode = request.LocaleCode,
                LanguageCode = request.LanguageCode,
                Name = request.Name,
                NativeName = request.NativeName,
                IsDefault = request.IsDefault,
                IsActive = request.IsActive,
                Direction = request.Direction ?? "ltr",
                DateFormat = request.DateFormat,
                CurrencyCode = request.CurrencyCode,
                DisplayOrder = request.DisplayOrder
            };

            // If this is set as default, unset other defaults
            if (language.IsDefault)
            {
                var otherDefaults = await _context.Languages
                    .Where(l => l.TenantId == request.TenantId && l.IsDefault)
                    .ToListAsync();

                foreach (var other in otherDefaults)
                {
                    other.IsDefault = false;
                }
            }

            _context.Languages.Add(language);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetLanguage), new { id = language.Id }, language);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating language");
            return BadRequest(new { error = ex.Message });
        }
    }

    // PUT: api/language/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateLanguage(Guid id, [FromBody] UpdateLanguageRequest request)
    {
        try
        {
            var language = await _context.Languages.FindAsync(id);

            if (language == null)
            {
                return NotFound(new { error = "Language not found" });
            }

            if (request.Name != null) language.Name = request.Name;
            if (request.NativeName != null) language.NativeName = request.NativeName;
            if (request.IsActive.HasValue) language.IsActive = request.IsActive.Value;
            if (request.Direction != null) language.Direction = request.Direction;
            if (request.DateFormat != null) language.DateFormat = request.DateFormat;
            if (request.CurrencyCode != null) language.CurrencyCode = request.CurrencyCode;
            if (request.DisplayOrder.HasValue) language.DisplayOrder = request.DisplayOrder.Value;

            // Handle default flag change
            if (request.IsDefault.HasValue && request.IsDefault.Value && !language.IsDefault)
            {
                var otherDefaults = await _context.Languages
                    .Where(l => l.TenantId == language.TenantId && l.IsDefault && l.Id != id)
                    .ToListAsync();

                foreach (var other in otherDefaults)
                {
                    other.IsDefault = false;
                }

                language.IsDefault = true;
            }

            await _context.SaveChangesAsync();

            return Ok(language);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating language {LanguageId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    // DELETE: api/language/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteLanguage(Guid id)
    {
        try
        {
            var language = await _context.Languages.FindAsync(id);

            if (language == null)
            {
                return NotFound(new { error = "Language not found" });
            }

            if (language.IsDefault)
            {
                return BadRequest(new { error = "Cannot delete the default language" });
            }

            _context.Languages.Remove(language);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting language {LanguageId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }
}

// Request DTOs
public class CreateLanguageRequest
{
    public string TenantId { get; set; } = "tenant-test";
    public string LocaleCode { get; set; } = string.Empty;
    public string LanguageCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? NativeName { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Direction { get; set; }
    public string? DateFormat { get; set; }
    public string? CurrencyCode { get; set; }
    public int DisplayOrder { get; set; }
}

public class UpdateLanguageRequest
{
    public string? Name { get; set; }
    public string? NativeName { get; set; }
    public bool? IsDefault { get; set; }
    public bool? IsActive { get; set; }
    public string? Direction { get; set; }
    public string? DateFormat { get; set; }
    public string? CurrencyCode { get; set; }
    public int? DisplayOrder { get; set; }
}
