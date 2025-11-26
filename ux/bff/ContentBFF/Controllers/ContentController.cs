using Microsoft.AspNetCore.Mvc;
using ContentBFF.Services;

namespace ContentBFF.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContentController : ControllerBase
{
    private readonly ContentServiceClient _contentService;
    private readonly ILogger<ContentController> _logger;

    public ContentController(ContentServiceClient contentService, ILogger<ContentController> logger)
    {
        _contentService = contentService;
        _logger = logger;
    }

    /// <summary>
    /// Get paginated content list
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetContent(
        [FromQuery] string? locale = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var response = await _contentService.GetContentAsync(locale, page, pageSize);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { error });
            }

            var content = await response.Content.ReadFromJsonAsync<object>();
            return Ok(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching content");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get content by slug
    /// </summary>
    [HttpGet("slug/{slug}")]
    public async Task<IActionResult> GetContentBySlug(string slug, [FromQuery] string? locale = null)
    {
        try
        {
            var response = await _contentService.GetContentBySlugAsync(slug, locale);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { error });
            }

            var content = await response.Content.ReadFromJsonAsync<object>();
            return Ok(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching content by slug {Slug}", slug);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get content by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetContentById(Guid id, [FromQuery] string? locale = null)
    {
        try
        {
            var response = await _contentService.GetContentByIdAsync(id, locale);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { error });
            }

            var content = await response.Content.ReadFromJsonAsync<object>();
            return Ok(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching content by id {Id}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get available languages
    /// </summary>
    [HttpGet("languages")]
    public async Task<IActionResult> GetLanguages()
    {
        try
        {
            var response = await _contentService.GetLanguagesAsync();

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { error });
            }

            var languages = await response.Content.ReadFromJsonAsync<object>();
            return Ok(languages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching languages");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get menu by code
    /// </summary>
    [HttpGet("menu/{code}")]
    public async Task<IActionResult> GetMenu(string code, [FromQuery] string? locale = null)
    {
        try
        {
            var response = await _contentService.GetMenuAsync(code, locale);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { error });
            }

            var menu = await response.Content.ReadFromJsonAsync<object>();
            return Ok(menu);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching menu {Code}", code);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get media by ID
    /// </summary>
    [HttpGet("media/{id:guid}")]
    public async Task<IActionResult> GetMedia(Guid id)
    {
        try
        {
            var response = await _contentService.GetMediaAsync(id);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { error });
            }

            var media = await response.Content.ReadFromJsonAsync<object>();
            return Ok(media);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching media {Id}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get all block content instances
    /// </summary>
    [HttpGet("blocks")]
    public async Task<IActionResult> GetBlockContents(
        [FromQuery] string? tenantId = null,
        [FromQuery] string? localeCode = null,
        [FromQuery] string? blockSlug = null)
    {
        try
        {
            var response = await _contentService.GetBlockContentsAsync(tenantId, localeCode, blockSlug);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { error });
            }

            var contents = await response.Content.ReadFromJsonAsync<object>();
            return Ok(contents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching block contents");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get block content by ID
    /// </summary>
    [HttpGet("blocks/{id:guid}")]
    public async Task<IActionResult> GetBlockContentById(Guid id, [FromQuery] string? localeCode = null)
    {
        try
        {
            var response = await _contentService.GetBlockContentByIdAsync(id, localeCode);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { error });
            }

            var content = await response.Content.ReadFromJsonAsync<object>();
            return Ok(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching block content {Id}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get block content by code/slug (primary endpoint for ContentBlock component)
    /// </summary>
    [HttpGet("blocks/code/{code}")]
    public async Task<IActionResult> GetBlockContentByCode(
        string code,
        [FromQuery] string? tenantId = null,
        [FromQuery] string? localeCode = null,
        [FromQuery] string? variant = null)
    {
        try
        {
            var response = await _contentService.GetBlockContentByCodeAsync(code, tenantId, localeCode, variant);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { error });
            }

            var content = await response.Content.ReadFromJsonAsync<object>();
            return Ok(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching block content by code {Code}", code);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get all block templates (schema definitions)
    /// </summary>
    [HttpGet("block-templates")]
    public async Task<IActionResult> GetBlockTemplates([FromQuery] string? tenantId = null)
    {
        try
        {
            var response = await _contentService.GetBlocksAsync(tenantId);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { error });
            }

            var blocks = await response.Content.ReadFromJsonAsync<object>();
            return Ok(blocks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching block templates");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get all section types
    /// </summary>
    [HttpGet("section-types")]
    public async Task<IActionResult> GetSectionTypes([FromQuery] string? tenantId = null)
    {
        try
        {
            var response = await _contentService.GetSectionTypesAsync(tenantId);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { error });
            }

            var sectionTypes = await response.Content.ReadFromJsonAsync<object>();
            return Ok(sectionTypes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching section types");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get variants for a specific block template
    /// </summary>
    [HttpGet("block-templates/{blockSlug}/variants")]
    public async Task<IActionResult> GetBlockVariants(string blockSlug, [FromQuery] string? tenantId = null)
    {
        try
        {
            var response = await _contentService.GetBlockVariantsAsync(blockSlug, tenantId);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { error });
            }

            var variants = await response.Content.ReadFromJsonAsync<object>();
            return Ok(variants);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching variants for block {BlockSlug}", blockSlug);
            return BadRequest(new { error = ex.Message });
        }
    }

    // ==========================================
    // BLOCK MANAGEMENT ENDPOINTS
    // ==========================================

    /// <summary>
    /// Create a new block template
    /// </summary>
    [HttpPost("block-templates")]
    public async Task<IActionResult> CreateBlock([FromBody] object block)
    {
        try
        {
            var response = await _contentService.CreateBlockAsync(block);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { error });
            }

            var created = await response.Content.ReadFromJsonAsync<object>();
            return Created("", created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating block");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Update an existing block template
    /// </summary>
    [HttpPut("block-templates/{id:guid}")]
    public async Task<IActionResult> UpdateBlock(Guid id, [FromBody] object block)
    {
        try
        {
            var response = await _contentService.UpdateBlockAsync(id, block);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { error });
            }

            var updated = await response.Content.ReadFromJsonAsync<object>();
            return Ok(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating block {BlockId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Delete a block template
    /// </summary>
    [HttpDelete("block-templates/{id:guid}")]
    public async Task<IActionResult> DeleteBlock(Guid id)
    {
        try
        {
            var response = await _contentService.DeleteBlockAsync(id);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { error });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting block {BlockId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    // ==========================================
    // BLOCK-SECTION MANAGEMENT ENDPOINTS
    // ==========================================

    /// <summary>
    /// Add a section type to a block
    /// </summary>
    [HttpPost("block-templates/{blockId:guid}/sections/{sectionTypeId:guid}")]
    public async Task<IActionResult> AddSectionToBlock(Guid blockId, Guid sectionTypeId, [FromBody] object? request = null)
    {
        try
        {
            var response = await _contentService.AddSectionToBlockAsync(blockId, sectionTypeId, request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { error });
            }

            var result = await response.Content.ReadFromJsonAsync<object>();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding section to block");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Remove a section type from a block
    /// </summary>
    [HttpDelete("block-templates/{blockId:guid}/sections/{sectionTypeId:guid}")]
    public async Task<IActionResult> RemoveSectionFromBlock(Guid blockId, Guid sectionTypeId)
    {
        try
        {
            var response = await _contentService.RemoveSectionFromBlockAsync(blockId, sectionTypeId);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { error });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing section from block");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Update section settings on a block
    /// </summary>
    [HttpPut("block-templates/{blockId:guid}/sections/{sectionTypeId:guid}")]
    public async Task<IActionResult> UpdateBlockSection(Guid blockId, Guid sectionTypeId, [FromBody] object request)
    {
        try
        {
            var response = await _contentService.UpdateBlockSectionAsync(blockId, sectionTypeId, request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { error });
            }

            var result = await response.Content.ReadFromJsonAsync<object>();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating block section");
            return BadRequest(new { error = ex.Message });
        }
    }

    // ==========================================
    // BLOCK CONTENT CRUD ENDPOINTS
    // ==========================================

    /// <summary>
    /// Create a new block content instance
    /// </summary>
    [HttpPost("blocks")]
    public async Task<IActionResult> CreateBlockContent([FromBody] object content)
    {
        try
        {
            var response = await _contentService.CreateBlockContentAsync(content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { error });
            }

            var created = await response.Content.ReadFromJsonAsync<object>();
            return Created("", created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating block content");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Update an existing block content instance
    /// </summary>
    [HttpPut("blocks/{id:guid}")]
    public async Task<IActionResult> UpdateBlockContent(Guid id, [FromBody] object content)
    {
        try
        {
            var response = await _contentService.UpdateBlockContentAsync(id, content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { error });
            }

            var updated = await response.Content.ReadFromJsonAsync<object>();
            return Ok(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating block content {ContentId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Delete a block content instance
    /// </summary>
    [HttpDelete("blocks/{id:guid}")]
    public async Task<IActionResult> DeleteBlockContent(Guid id)
    {
        try
        {
            var response = await _contentService.DeleteBlockContentAsync(id);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { error });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting block content {ContentId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get block content for editing (with all translations)
    /// </summary>
    [HttpGet("blocks/{id:guid}/edit")]
    public async Task<IActionResult> GetBlockContentForEdit(Guid id)
    {
        try
        {
            var response = await _contentService.GetBlockContentForEditAsync(id);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { error });
            }

            var content = await response.Content.ReadFromJsonAsync<object>();
            return Ok(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching block content for edit {ContentId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }
}
