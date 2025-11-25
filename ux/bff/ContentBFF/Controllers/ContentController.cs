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
    /// Get all content blocks
    /// </summary>
    [HttpGet("blocks")]
    public async Task<IActionResult> GetContentBlocks(
        [FromQuery] string? tenantId = null,
        [FromQuery] string? localeCode = null,
        [FromQuery] string? blockType = null,
        [FromQuery] bool? isGlobal = null)
    {
        try
        {
            var response = await _contentService.GetContentBlocksAsync(tenantId, localeCode, blockType, isGlobal);

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
            _logger.LogError(ex, "Error fetching content blocks");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get content block by ID
    /// </summary>
    [HttpGet("blocks/{id:guid}")]
    public async Task<IActionResult> GetContentBlockById(Guid id, [FromQuery] string? localeCode = null)
    {
        try
        {
            var response = await _contentService.GetContentBlockByIdAsync(id, localeCode);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { error });
            }

            var block = await response.Content.ReadFromJsonAsync<object>();
            return Ok(block);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching content block {Id}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get content block by code
    /// </summary>
    [HttpGet("blocks/code/{code}")]
    public async Task<IActionResult> GetContentBlockByCode(
        string code,
        [FromQuery] string? tenantId = null,
        [FromQuery] string? localeCode = null)
    {
        try
        {
            var response = await _contentService.GetContentBlockByCodeAsync(code, tenantId, localeCode);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { error });
            }

            var block = await response.Content.ReadFromJsonAsync<object>();
            return Ok(block);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching content block by code {Code}", code);
            return BadRequest(new { error = ex.Message });
        }
    }
}
