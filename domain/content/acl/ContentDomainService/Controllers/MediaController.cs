using ContentDomainService.Data;
using ContentDomainService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContentDomainService.Controllers;

[ApiController]
[Route("api/media")]
public class MediaController : ControllerBase
{
    private readonly ContentDbContext _context;
    private readonly ILogger<MediaController> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _uploadPath;
    private readonly string _baseUrl;

    public MediaController(
        ContentDbContext context,
        ILogger<MediaController> logger,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
        _uploadPath = configuration["UPLOAD_PATH"] ?? "/app/uploads";
        _baseUrl = configuration["MEDIA_BASE_URL"] ?? "http://localhost:3460/uploads";
    }

    /// <summary>
    /// Checks if the caller is a System Admin via the X-Is-System-Admin header
    /// </summary>
    private bool IsCallerSystemAdmin()
    {
        return Request.Headers.TryGetValue("X-Is-System-Admin", out var value)
            && value.FirstOrDefault()?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
    }

    /// <summary>
    /// Gets the tenant ID from header
    /// Returns null if empty GUID (All Tenants mode for System Admins)
    /// </summary>
    private string? GetHeaderTenantId()
    {
        if (Request.Headers.TryGetValue("X-Tenant-ID", out var headerValue))
        {
            var headerTenantId = headerValue.FirstOrDefault();
            if (!string.IsNullOrEmpty(headerTenantId))
            {
                // Empty GUID means "All Tenants" mode - return null to skip filtering
                if (headerTenantId == "00000000-0000-0000-0000-000000000000")
                {
                    _logger.LogDebug("All Tenants mode detected from header");
                    return null;
                }
                return headerTenantId;
            }
        }
        return null;
    }

    /// <summary>
    /// Gets the tenant ID, requiring it unless in System Admin "All Tenants" mode
    /// Query param tenantId takes precedence for filtering (from dropdown)
    /// </summary>
    private (string? tenantId, bool isAllTenantsMode) GetTenantContext(string? queryTenantId)
    {
        var headerTenantId = GetHeaderTenantId();
        var isAllTenantsMode = IsCallerSystemAdmin() && headerTenantId == null;

        // Query param takes precedence (explicit filter from dropdown)
        // Then fall back to header tenant
        var effectiveTenantId = queryTenantId ?? headerTenantId;

        // If we have a tenantId, use it for filtering (even in All Tenants mode)
        if (!string.IsNullOrEmpty(effectiveTenantId))
        {
            return (effectiveTenantId, isAllTenantsMode);
        }

        // If in All Tenants mode with no filter, return null to show all
        if (isAllTenantsMode)
        {
            return (null, true);
        }

        // Non-admin with no tenant - use default for backward compat
        return ("default", false);
    }

    // GET: api/media
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Media>>> GetMedia(
        [FromQuery] string? tenantId = null,
        [FromQuery] string? mediaType = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var query = _context.MediaItems.AsQueryable();

            if (!string.IsNullOrEmpty(tenantId))
            {
                query = query.Where(m => m.TenantId == tenantId);
            }

            if (!string.IsNullOrEmpty(mediaType))
            {
                query = query.Where(m => m.MediaType == mediaType);
            }

            var media = await query
                .OrderByDescending(m => m.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(media);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching media");
            return BadRequest(new { error = ex.Message });
        }
    }

    // GET: api/media/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Media>> GetMediaById(Guid id)
    {
        try
        {
            var (effectiveTenantId, isAllTenantsMode) = GetTenantContext(null);

            var query = _context.MediaItems
                .Include(m => m.Translations)
                .Where(m => m.Id == id);

            // Apply tenant filter if a tenantId is provided
            if (effectiveTenantId != null)
            {
                query = query.Where(m => m.TenantId == effectiveTenantId);
            }

            var media = await query.FirstOrDefaultAsync();

            if (media == null)
            {
                return NotFound(new { error = "Media not found" });
            }

            return Ok(media);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching media {MediaId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    // POST: api/media/upload
    [HttpPost("upload")]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10MB limit
    public async Task<ActionResult<Media>> UploadMedia(
        IFormFile file,
        [FromForm] string? tenantId = "default",
        [FromForm] Guid? folderId = null,
        [FromForm] Guid? uploadedBy = null,
        [FromForm] bool isPublic = true)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "No file provided" });
            }

            // Validate file type
            var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp", "image/svg+xml" };
            if (!allowedTypes.Contains(file.ContentType.ToLower()))
            {
                return BadRequest(new { error = $"File type not allowed. Allowed types: {string.Join(", ", allowedTypes)}" });
            }

            // Generate unique filename
            var fileExtension = Path.GetExtension(file.FileName).ToLower();
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var relativePath = $"{tenantId}/{DateTime.UtcNow:yyyy/MM}";
            var fullPath = Path.Combine(_uploadPath, relativePath);

            // Ensure directory exists
            Directory.CreateDirectory(fullPath);

            var filePath = Path.Combine(fullPath, uniqueFileName);
            var fileUrl = $"{_baseUrl}/{relativePath}/{uniqueFileName}";

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Determine media type
            var mediaType = file.ContentType.StartsWith("image/") ? "image" :
                           file.ContentType.StartsWith("video/") ? "video" :
                           file.ContentType.StartsWith("audio/") ? "audio" : "document";

            // Get image dimensions if applicable
            int? width = null;
            int? height = null;
            if (mediaType == "image" && file.ContentType != "image/svg+xml")
            {
                try
                {
                    using var image = await SixLabors.ImageSharp.Image.LoadAsync(filePath);
                    width = image.Width;
                    height = image.Height;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not read image dimensions");
                }
            }

            // Create media record
            var media = new Media
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId ?? "default",
                FolderId = folderId,
                FileName = uniqueFileName,
                OriginalFileName = file.FileName,
                FilePath = filePath,
                FileUrl = fileUrl,
                MimeType = file.ContentType,
                FileSize = file.Length,
                FileExtension = fileExtension,
                MediaType = mediaType,
                Width = width,
                Height = height,
                IsPublic = isPublic,
                UploadedBy = uploadedBy,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.MediaItems.Add(media);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Uploaded media {MediaId}: {FileName}", media.Id, file.FileName);

            return CreatedAtAction(nameof(GetMediaById), new { id = media.Id }, media);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading media");
            return BadRequest(new { error = ex.Message });
        }
    }

    // DELETE: api/media/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteMedia(Guid id)
    {
        try
        {
            var (effectiveTenantId, isAllTenantsMode) = GetTenantContext(null);

            var query = _context.MediaItems.Where(m => m.Id == id);

            // Apply tenant filter if a tenantId is provided
            if (effectiveTenantId != null)
            {
                query = query.Where(m => m.TenantId == effectiveTenantId);
            }

            var media = await query.FirstOrDefaultAsync();

            if (media == null)
            {
                return NotFound(new { error = "Media not found" });
            }

            // Delete file from disk
            if (System.IO.File.Exists(media.FilePath))
            {
                System.IO.File.Delete(media.FilePath);
            }

            _context.MediaItems.Remove(media);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted media {MediaId}", id);

            return Ok(new { message = "Media deleted successfully", id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting media {MediaId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }
}
