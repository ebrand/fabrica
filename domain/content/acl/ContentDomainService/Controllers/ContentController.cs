using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ContentDomainService.Data;
using ContentDomainService.Models;

namespace ContentDomainService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContentController : ControllerBase
{
    private readonly ContentDbContext _context;
    private readonly ILogger<ContentController> _logger;

    public ContentController(ContentDbContext context, ILogger<ContentController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/content
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetContents(
        [FromQuery] string tenantId = "tenant-test",
        [FromQuery] string? localeCode = "en-US",
        [FromQuery] string? contentType = null,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var query = _context.Contents
                .Include(c => c.ContentType)
                .Include(c => c.Translations)
                .Include(c => c.FeaturedImage)
                .Where(c => c.TenantId == tenantId);

            if (!string.IsNullOrEmpty(contentType))
            {
                query = query.Where(c => c.ContentType != null && c.ContentType.Code == contentType);
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(c => c.Status == status);
            }

            var totalCount = await query.CountAsync();

            var contents = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new
                {
                    c.Id,
                    c.TenantId,
                    c.Slug,
                    c.Status,
                    c.Visibility,
                    c.IsFeatured,
                    c.IsPinned,
                    c.ViewCount,
                    c.PublishAt,
                    c.PublishedAt,
                    c.CreatedAt,
                    c.UpdatedAt,
                    ContentType = c.ContentType == null ? null : new
                    {
                        c.ContentType.Id,
                        c.ContentType.Code,
                        c.ContentType.Name
                    },
                    FeaturedImage = c.FeaturedImage == null ? null : new
                    {
                        c.FeaturedImage.Id,
                        c.FeaturedImage.FileUrl,
                        c.FeaturedImage.ThumbnailUrl
                    },
                    Translation = c.Translations
                        .Where(t => t.LocaleCode == localeCode)
                        .Select(t => new
                        {
                            t.Title,
                            t.Excerpt,
                            t.SeoTitle,
                            t.SeoDescription,
                            t.TranslationStatus
                        })
                        .FirstOrDefault()
                })
                .ToListAsync();

            return Ok(new
            {
                data = contents,
                pagination = new
                {
                    page,
                    pageSize,
                    totalCount,
                    totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting contents");
            return BadRequest(new { error = ex.Message });
        }
    }

    // GET: api/content/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetContent(Guid id, [FromQuery] string? localeCode = null)
    {
        try
        {
            var content = await _context.Contents
                .Include(c => c.ContentType)
                .Include(c => c.Translations)
                .Include(c => c.FeaturedImage)
                    .ThenInclude(m => m!.Translations)
                .Include(c => c.CategoryMappings)
                    .ThenInclude(cm => cm.ContentCategory)
                        .ThenInclude(cc => cc!.Translations)
                .Include(c => c.TagMappings)
                    .ThenInclude(tm => tm.ContentTag)
                        .ThenInclude(ct => ct!.Translations)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (content == null)
            {
                return NotFound(new { error = "Content not found" });
            }

            var translations = localeCode != null
                ? content.Translations.Where(t => t.LocaleCode == localeCode).ToList()
                : content.Translations.ToList();

            return Ok(new
            {
                content.Id,
                content.TenantId,
                content.Slug,
                content.Status,
                content.Visibility,
                content.IsFeatured,
                content.IsPinned,
                content.ViewCount,
                content.PublishAt,
                content.PublishedAt,
                content.CustomData,
                content.CreatedAt,
                content.UpdatedAt,
                ContentType = content.ContentType == null ? null : new
                {
                    content.ContentType.Id,
                    content.ContentType.Code,
                    content.ContentType.Name
                },
                FeaturedImage = content.FeaturedImage == null ? null : new
                {
                    content.FeaturedImage.Id,
                    content.FeaturedImage.FileUrl,
                    content.FeaturedImage.ThumbnailUrl,
                    Translations = content.FeaturedImage.Translations.Select(t => new
                    {
                        t.LocaleCode,
                        t.AltText,
                        t.Title,
                        t.Caption
                    })
                },
                Translations = translations.Select(t => new
                {
                    t.Id,
                    t.LocaleCode,
                    t.Title,
                    t.Excerpt,
                    t.Body,
                    t.SeoTitle,
                    t.SeoDescription,
                    t.SeoKeywords,
                    t.OgTitle,
                    t.OgDescription,
                    t.OgImageUrl,
                    t.TranslationStatus
                }),
                Categories = content.CategoryMappings.Select(cm => new
                {
                    cm.ContentCategory!.Id,
                    cm.ContentCategory.Slug,
                    cm.IsPrimary,
                    Translations = cm.ContentCategory.Translations.Select(t => new
                    {
                        t.LocaleCode,
                        t.Name,
                        t.Description
                    })
                }),
                Tags = content.TagMappings.Select(tm => new
                {
                    tm.ContentTag!.Id,
                    tm.ContentTag.Slug,
                    Translations = tm.ContentTag.Translations.Select(t => new
                    {
                        t.LocaleCode,
                        t.Name
                    })
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting content {ContentId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    // GET: api/content/slug/{slug}
    [HttpGet("slug/{slug}")]
    public async Task<ActionResult<object>> GetContentBySlug(
        string slug,
        [FromQuery] string tenantId = "tenant-test",
        [FromQuery] string? localeCode = "en-US")
    {
        try
        {
            var content = await _context.Contents
                .Include(c => c.ContentType)
                .Include(c => c.Translations)
                .Include(c => c.FeaturedImage)
                .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Slug == slug);

            if (content == null)
            {
                return NotFound(new { error = "Content not found" });
            }

            var translation = content.Translations
                .FirstOrDefault(t => t.LocaleCode == localeCode)
                ?? content.Translations.FirstOrDefault();

            return Ok(new
            {
                content.Id,
                content.Slug,
                content.Status,
                content.PublishedAt,
                ContentType = content.ContentType?.Code,
                Title = translation?.Title,
                Excerpt = translation?.Excerpt,
                Body = translation?.Body,
                SeoTitle = translation?.SeoTitle,
                SeoDescription = translation?.SeoDescription,
                FeaturedImageUrl = content.FeaturedImage?.FileUrl
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting content by slug {Slug}", slug);
            return BadRequest(new { error = ex.Message });
        }
    }

    // POST: api/content
    [HttpPost]
    public async Task<ActionResult<Content>> CreateContent([FromBody] CreateContentRequest request)
    {
        try
        {
            var contentType = await _context.ContentTypes
                .FirstOrDefaultAsync(ct => ct.TenantId == request.TenantId && ct.Code == request.ContentTypeCode);

            if (contentType == null)
            {
                return BadRequest(new { error = "Content type not found" });
            }

            var content = new Content
            {
                TenantId = request.TenantId,
                ContentTypeId = contentType.Id,
                Slug = request.Slug,
                Status = request.Status ?? "draft",
                Visibility = request.Visibility ?? "public",
                IsFeatured = request.IsFeatured,
                PublishAt = request.PublishAt,
                CustomData = request.CustomData
            };

            _context.Contents.Add(content);

            // Add translations
            if (request.Translations != null)
            {
                foreach (var translationRequest in request.Translations)
                {
                    var translation = new ContentTranslation
                    {
                        ContentId = content.Id,
                        LocaleCode = translationRequest.LocaleCode,
                        Title = translationRequest.Title,
                        Excerpt = translationRequest.Excerpt,
                        Body = translationRequest.Body,
                        SeoTitle = translationRequest.SeoTitle,
                        SeoDescription = translationRequest.SeoDescription,
                        TranslationStatus = translationRequest.TranslationStatus ?? "draft"
                    };
                    content.Translations.Add(translation);
                }
            }

            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetContent), new { id = content.Id }, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating content");
            return BadRequest(new { error = ex.Message });
        }
    }

    // PUT: api/content/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateContent(Guid id, [FromBody] UpdateContentRequest request)
    {
        try
        {
            var content = await _context.Contents
                .Include(c => c.Translations)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (content == null)
            {
                return NotFound(new { error = "Content not found" });
            }

            if (request.Slug != null) content.Slug = request.Slug;
            if (request.Status != null) content.Status = request.Status;
            if (request.Visibility != null) content.Visibility = request.Visibility;
            if (request.IsFeatured.HasValue) content.IsFeatured = request.IsFeatured.Value;
            if (request.IsPinned.HasValue) content.IsPinned = request.IsPinned.Value;
            if (request.PublishAt.HasValue) content.PublishAt = request.PublishAt;
            if (request.CustomData != null) content.CustomData = request.CustomData;

            // Handle publish status change
            if (request.Status == "published" && content.PublishedAt == null)
            {
                content.PublishedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return Ok(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating content {ContentId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    // DELETE: api/content/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteContent(Guid id)
    {
        try
        {
            var content = await _context.Contents.FindAsync(id);

            if (content == null)
            {
                return NotFound(new { error = "Content not found" });
            }

            _context.Contents.Remove(content);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting content {ContentId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }
}

// Request DTOs
public class CreateContentRequest
{
    public string TenantId { get; set; } = "tenant-test";
    public string ContentTypeCode { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Status { get; set; }
    public string? Visibility { get; set; }
    public bool IsFeatured { get; set; }
    public DateTime? PublishAt { get; set; }
    public string? CustomData { get; set; }
    public List<CreateTranslationRequest>? Translations { get; set; }
}

public class CreateTranslationRequest
{
    public string LocaleCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Excerpt { get; set; }
    public string? Body { get; set; }
    public string? SeoTitle { get; set; }
    public string? SeoDescription { get; set; }
    public string? TranslationStatus { get; set; }
}

public class UpdateContentRequest
{
    public string? Slug { get; set; }
    public string? Status { get; set; }
    public string? Visibility { get; set; }
    public bool? IsFeatured { get; set; }
    public bool? IsPinned { get; set; }
    public DateTime? PublishAt { get; set; }
    public string? CustomData { get; set; }
}
