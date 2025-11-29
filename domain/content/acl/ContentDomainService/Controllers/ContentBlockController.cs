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
        return ("tenant-test", false);
    }

    /// <summary>
    /// Get all block content for a tenant
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetBlockContents(
        [FromQuery] string? tenantId = null,
        [FromQuery] string? localeCode = "en-US",
        [FromQuery] string? blockSlug = null)
    {
        try
        {
            var (effectiveTenantId, isAllTenantsMode) = GetTenantContext(tenantId);

            // For language, use a specific tenant or default
            var languageTenantId = effectiveTenantId ?? tenantId;
            var language = await _context.Languages
                .FirstOrDefaultAsync(l => l.TenantId == languageTenantId && l.LocaleCode == localeCode);

            if (language == null && !isAllTenantsMode)
            {
                return BadRequest(new { error = $"Language '{localeCode}' not found for tenant" });
            }

            var query = _context.BlockContents
                .Include(bc => bc.Block)
                .Include(bc => bc.DefaultVariant)
                .Include(bc => bc.SectionTranslations)
                    .ThenInclude(t => t.SectionType)
                .Where(bc => bc.IsActive);

            // Apply tenant filter if a specific tenant is selected
            if (effectiveTenantId != null)
            {
                query = query.Where(bc => bc.TenantId == effectiveTenantId);
                _logger.LogInformation("Filtering block contents by tenant {TenantId}", effectiveTenantId);
            }
            else if (isAllTenantsMode)
            {
                _logger.LogInformation("System Admin viewing all block contents (All Tenants mode)");
            }

            if (!string.IsNullOrEmpty(blockSlug))
            {
                query = query.Where(bc => bc.Block!.Slug == blockSlug);
            }

            var contents = await query
                .OrderBy(bc => bc.Name)
                .ToListAsync();

            var result = contents.Select(bc => new
            {
                bc.ContentId,
                bc.TenantId,
                bc.Slug,
                bc.Name,
                bc.Description,
                Block = new { bc.Block!.Slug, bc.Block.Name },
                DefaultVariant = bc.DefaultVariant != null ? new { bc.DefaultVariant.Slug, bc.DefaultVariant.Name } : null,
                Sections = language != null ? bc.SectionTranslations
                    .Where(t => t.LanguageId == language.Id)
                    .ToDictionary(t => t.SectionType!.Slug, t => t.Content) : new Dictionary<string, string?>(),
                bc.CreatedAt,
                bc.UpdatedAt
            });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting block contents");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get block content by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetBlockContent(Guid id, [FromQuery] string? localeCode = "en-US")
    {
        try
        {
            var (effectiveTenantId, isAllTenantsMode) = GetTenantContext(null);

            var query = _context.BlockContents
                .Include(bc => bc.Block)
                    .ThenInclude(b => b!.BlockSections)
                    .ThenInclude(bs => bs.SectionType)
                .Include(bc => bc.DefaultVariant)
                .Include(bc => bc.SectionTranslations)
                    .ThenInclude(t => t.SectionType)
                .Include(bc => bc.SectionTranslations)
                    .ThenInclude(t => t.Language)
                .Where(bc => bc.ContentId == id);

            // Apply tenant filter if a tenantId is provided
            if (effectiveTenantId != null)
            {
                query = query.Where(bc => bc.TenantId == effectiveTenantId);
            }

            var content = await query.FirstOrDefaultAsync();

            if (content == null)
            {
                return NotFound(new { error = "Block content not found" });
            }

            // Get default language for tenant if no specific locale requested
            var language = await _context.Languages
                .FirstOrDefaultAsync(l => l.TenantId == content.TenantId && l.LocaleCode == localeCode);

            return Ok(BuildContentResponse(content, language?.Id));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting block content {ContentId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get block content by slug (primary endpoint for frontend)
    /// Usage: GET /api/contentblock/code/hero-welcome?localeCode=en-US&variant=hero
    /// </summary>
    [HttpGet("code/{slug}")]
    public async Task<ActionResult<object>> GetBlockContentBySlug(
        string slug,
        [FromQuery] string? tenantId = null,
        [FromQuery] string? localeCode = "en-US",
        [FromQuery] string? variant = null)
    {
        try
        {
            // Get language ID for locale
            var language = await _context.Languages
                .FirstOrDefaultAsync(l => l.TenantId == tenantId && l.LocaleCode == localeCode);

            if (language == null)
            {
                // Fall back to default language
                language = await _context.Languages
                    .FirstOrDefaultAsync(l => l.TenantId == tenantId && l.IsDefault);

                if (language == null)
                {
                    return BadRequest(new { error = $"No language found for tenant '{tenantId}'" });
                }
            }

            var content = await _context.BlockContents
                .Include(bc => bc.Block)
                    .ThenInclude(b => b!.Variants)
                .Include(bc => bc.Block)
                    .ThenInclude(b => b!.BlockSections)
                    .ThenInclude(bs => bs.SectionType)
                .Include(bc => bc.DefaultVariant)
                .Include(bc => bc.SectionTranslations)
                    .ThenInclude(t => t.SectionType)
                .FirstOrDefaultAsync(bc => bc.TenantId == tenantId && bc.Slug == slug);

            if (content == null)
            {
                return NotFound(new { error = $"Block content '{slug}' not found" });
            }

            // Determine which variant to use
            Variant? selectedVariant = null;
            if (!string.IsNullOrEmpty(variant))
            {
                selectedVariant = content.Block?.Variants.FirstOrDefault(v => v.Slug == variant);
            }
            selectedVariant ??= content.DefaultVariant;
            selectedVariant ??= content.Block?.Variants.FirstOrDefault(v => v.IsDefault);

            // Build sections dictionary from translations
            var sections = content.SectionTranslations
                .Where(t => t.LanguageId == language.Id)
                .ToDictionary(t => t.SectionType!.Slug, t => t.Content);

            // Build response in format expected by ContentBlock component
            return Ok(new
            {
                content.ContentId,
                content.Slug,
                content.Name,
                Block = content.Block!.Slug,
                Variant = selectedVariant?.Slug ?? "default",
                VariantName = selectedVariant?.Name ?? "Default",
                LocaleCode = language.LocaleCode,
                // Section values as flat properties for easy access
                Sections = sections,
                // Also expose common fields directly for backwards compatibility
                Title = sections.GetValueOrDefault("title"),
                Subtitle = sections.GetValueOrDefault("subtitle"),
                Body = sections.GetValueOrDefault("body"),
                Author = sections.GetValueOrDefault("author"),
                CtaText = sections.GetValueOrDefault("cta-text"),
                CtaUrl = sections.GetValueOrDefault("cta-url"),
                ImageUrl = sections.GetValueOrDefault("image-url")
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting block content by slug {Slug}", slug);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get all blocks (templates) for a tenant
    /// </summary>
    [HttpGet("blocks")]
    public async Task<ActionResult<IEnumerable<object>>> GetBlocks(
        [FromQuery] string? tenantId = null)
    {
        try
        {
            var (effectiveTenantId, isAllTenantsMode) = GetTenantContext(tenantId);

            var query = _context.Blocks
                .Include(b => b.BlockSections)
                    .ThenInclude(bs => bs.SectionType)
                .Include(b => b.Variants)
                .Where(b => b.IsActive);

            // Apply tenant filter if a specific tenant is selected
            if (effectiveTenantId != null)
            {
                query = query.Where(b => b.TenantId == effectiveTenantId);
                _logger.LogInformation("Filtering blocks by tenant {TenantId}", effectiveTenantId);
            }
            else if (isAllTenantsMode)
            {
                _logger.LogInformation("System Admin viewing all blocks (All Tenants mode)");
            }

            var blocks = await query
                .OrderBy(b => b.DisplayOrder)
                .ToListAsync();

            var result = blocks.Select(b => new
            {
                b.BlockId,
                b.TenantId,
                b.Name,
                b.Slug,
                b.Description,
                b.Icon,
                Sections = b.BlockSections
                    .OrderBy(bs => bs.DisplayOrder)
                    .Select(bs => new
                    {
                        bs.SectionType!.Slug,
                        bs.SectionType.Name,
                        bs.SectionType.FieldType,
                        bs.IsRequired,
                        bs.DisplayOrder
                    }),
                Variants = b.Variants
                    .Where(v => v.IsActive)
                    .OrderBy(v => v.DisplayOrder)
                    .Select(v => new
                    {
                        v.VariantId,
                        v.Name,
                        v.Slug,
                        v.Description,
                        v.IsDefault,
                        v.PreviewImageUrl
                    })
            });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting blocks");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get all section types for a tenant
    /// </summary>
    [HttpGet("section-types")]
    public async Task<ActionResult<IEnumerable<object>>> GetSectionTypes(
        [FromQuery] string? tenantId = null)
    {
        try
        {
            var (effectiveTenantId, isAllTenantsMode) = GetTenantContext(tenantId);

            var query = _context.SectionTypes.Where(st => st.IsActive);

            // Apply tenant filter if a specific tenant is selected
            if (effectiveTenantId != null)
            {
                query = query.Where(st => st.TenantId == effectiveTenantId);
                _logger.LogInformation("Filtering section types by tenant {TenantId}", effectiveTenantId);
            }
            else if (isAllTenantsMode)
            {
                _logger.LogInformation("System Admin viewing all section types (All Tenants mode)");
            }

            var sectionTypes = await query
                .OrderBy(st => st.DisplayOrder)
                .Select(st => new
                {
                    st.SectionTypeId,
                    st.TenantId,
                    st.Name,
                    st.Slug,
                    st.Description,
                    st.FieldType
                })
                .ToListAsync();

            return Ok(sectionTypes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting section types");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Create a new section type
    /// </summary>
    [HttpPost("section-types")]
    public async Task<ActionResult<object>> CreateSectionType([FromBody] CreateSectionTypeRequest request)
    {
        try
        {
            var tenantId = request.TenantId ?? GetHeaderTenantId() ?? "tenant-test";

            // Check slug uniqueness
            var existingSlug = await _context.SectionTypes
                .AnyAsync(st => st.TenantId == tenantId && st.Slug == request.Slug);

            if (existingSlug)
            {
                return BadRequest(new { error = $"Section type with slug '{request.Slug}' already exists" });
            }

            // Get max display order
            var maxOrder = await _context.SectionTypes
                .Where(st => st.TenantId == tenantId)
                .MaxAsync(st => (int?)st.DisplayOrder) ?? 0;

            var sectionType = new SectionType
            {
                TenantId = tenantId,
                Name = request.Name,
                Slug = request.Slug,
                Description = request.Description,
                FieldType = request.FieldType ?? "text",
                ValidationRules = request.ValidationRules,
                IsActive = true,
                DisplayOrder = request.DisplayOrder ?? maxOrder + 1
            };

            _context.SectionTypes.Add(sectionType);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created section type {SectionTypeId} with slug {Slug}", sectionType.SectionTypeId, sectionType.Slug);

            return CreatedAtAction(nameof(GetSectionTypes), new
            {
                sectionType.SectionTypeId,
                sectionType.Name,
                sectionType.Slug,
                sectionType.Description,
                sectionType.FieldType,
                sectionType.DisplayOrder
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating section type");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Update an existing section type
    /// </summary>
    [HttpPut("section-types/{id}")]
    public async Task<ActionResult<object>> UpdateSectionType(Guid id, [FromBody] UpdateSectionTypeRequest request)
    {
        try
        {
            var (effectiveTenantId, isAllTenantsMode) = GetTenantContext(null);

            var query = _context.SectionTypes.Where(st => st.SectionTypeId == id);

            // Apply tenant filter if a tenantId is provided
            if (effectiveTenantId != null)
            {
                query = query.Where(st => st.TenantId == effectiveTenantId);
            }

            var sectionType = await query.FirstOrDefaultAsync();
            if (sectionType == null)
            {
                return NotFound(new { error = "Section type not found" });
            }

            // Check slug uniqueness if changed
            if (!string.IsNullOrEmpty(request.Slug) && request.Slug != sectionType.Slug)
            {
                var existingSlug = await _context.SectionTypes
                    .AnyAsync(st => st.TenantId == sectionType.TenantId && st.Slug == request.Slug && st.SectionTypeId != id);

                if (existingSlug)
                {
                    return BadRequest(new { error = $"Section type with slug '{request.Slug}' already exists" });
                }
                sectionType.Slug = request.Slug;
            }

            if (!string.IsNullOrEmpty(request.Name)) sectionType.Name = request.Name;
            if (request.Description != null) sectionType.Description = request.Description;
            if (!string.IsNullOrEmpty(request.FieldType)) sectionType.FieldType = request.FieldType;
            if (request.ValidationRules != null) sectionType.ValidationRules = request.ValidationRules;
            if (request.DisplayOrder.HasValue) sectionType.DisplayOrder = request.DisplayOrder.Value;
            if (request.IsActive.HasValue) sectionType.IsActive = request.IsActive.Value;

            sectionType.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated section type {SectionTypeId}", id);

            return Ok(new
            {
                sectionType.SectionTypeId,
                sectionType.Name,
                sectionType.Slug,
                sectionType.Description,
                sectionType.FieldType,
                sectionType.DisplayOrder,
                sectionType.IsActive
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating section type {SectionTypeId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Delete a section type
    /// </summary>
    [HttpDelete("section-types/{id}")]
    public async Task<ActionResult> DeleteSectionType(Guid id)
    {
        try
        {
            var (effectiveTenantId, isAllTenantsMode) = GetTenantContext(null);

            var query = _context.SectionTypes
                .Include(st => st.BlockSections)
                .Where(st => st.SectionTypeId == id);

            // Apply tenant filter if a tenantId is provided
            if (effectiveTenantId != null)
            {
                query = query.Where(st => st.TenantId == effectiveTenantId);
            }

            var sectionType = await query.FirstOrDefaultAsync();

            if (sectionType == null)
            {
                return NotFound(new { error = "Section type not found" });
            }

            // Check if in use by any blocks
            if (sectionType.BlockSections.Any())
            {
                return BadRequest(new { error = $"Cannot delete section type - it is used by {sectionType.BlockSections.Count} block(s). Remove it from all blocks first." });
            }

            // Check if any translations exist
            var hasTranslations = await _context.BlockContentSectionTranslations
                .AnyAsync(t => t.SectionTypeId == id);

            if (hasTranslations)
            {
                return BadRequest(new { error = "Cannot delete section type - it has content translations. Delete the content first." });
            }

            _context.SectionTypes.Remove(sectionType);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted section type {SectionTypeId}", id);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting section type {SectionTypeId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get variants for a specific block
    /// </summary>
    [HttpGet("blocks/{blockSlug}/variants")]
    public async Task<ActionResult<IEnumerable<object>>> GetBlockVariants(
        string blockSlug,
        [FromQuery] string? tenantId = null)
    {
        try
        {
            var block = await _context.Blocks
                .Include(b => b.Variants)
                .FirstOrDefaultAsync(b => b.TenantId == tenantId && b.Slug == blockSlug);

            if (block == null)
            {
                return NotFound(new { error = $"Block '{blockSlug}' not found" });
            }

            var variants = block.Variants
                .Where(v => v.IsActive)
                .OrderBy(v => v.DisplayOrder)
                .Select(v => new
                {
                    v.VariantId,
                    v.Name,
                    v.Slug,
                    v.Description,
                    v.IsDefault,
                    v.PreviewImageUrl,
                    v.CssClass
                });

            return Ok(variants);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting variants for block {BlockSlug}", blockSlug);
            return BadRequest(new { error = ex.Message });
        }
    }

    // ==========================================
    // BLOCK CONTENT CRUD ENDPOINTS
    // ==========================================

    /// <summary>
    /// Create a new block content instance
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<object>> CreateBlockContent([FromBody] CreateBlockContentRequest request)
    {
        try
        {
            var tenantId = request.TenantId ?? GetHeaderTenantId() ?? "tenant-test";

            // Validate block exists
            var block = await _context.Blocks
                .Include(b => b.BlockSections)
                    .ThenInclude(bs => bs.SectionType)
                .FirstOrDefaultAsync(b => b.TenantId == tenantId && b.BlockId == request.BlockId);

            if (block == null)
            {
                return BadRequest(new { error = "Block not found" });
            }

            // Validate slug uniqueness
            var existingSlug = await _context.BlockContents
                .AnyAsync(bc => bc.TenantId == tenantId && bc.Slug == request.Slug);

            if (existingSlug)
            {
                return BadRequest(new { error = $"Content with slug '{request.Slug}' already exists" });
            }

            // Validate variant if provided
            Variant? variant = null;
            if (request.DefaultVariantId.HasValue)
            {
                variant = await _context.Variants
                    .FirstOrDefaultAsync(v => v.VariantId == request.DefaultVariantId && v.BlockId == request.BlockId);

                if (variant == null)
                {
                    return BadRequest(new { error = "Variant not found or doesn't belong to this block" });
                }
            }

            // Create block content
            var blockContent = new BlockContent
            {
                TenantId = tenantId,
                BlockId = request.BlockId,
                DefaultVariantId = request.DefaultVariantId,
                Slug = request.Slug,
                Name = request.Name,
                Description = request.Description,
                AccessControl = request.AccessControl ?? "everyone",
                Visibility = request.Visibility ?? "public",
                PublishAt = request.PublishAt,
                UnpublishAt = request.UnpublishAt,
                IsActive = request.IsActive ?? true
            };

            _context.BlockContents.Add(blockContent);
            await _context.SaveChangesAsync();

            // Create section translations if provided
            if (request.Translations != null)
            {
                foreach (var translation in request.Translations)
                {
                    // Get language
                    var language = await _context.Languages
                        .FirstOrDefaultAsync(l => l.TenantId == tenantId && l.LocaleCode == translation.LocaleCode);

                    if (language == null)
                    {
                        _logger.LogWarning("Language '{LocaleCode}' not found, skipping translations", translation.LocaleCode);
                        continue;
                    }

                    // Create translations for each section
                    foreach (var section in translation.Sections)
                    {
                        var sectionType = block.BlockSections
                            .FirstOrDefault(bs => bs.SectionType?.Slug == section.Key)?.SectionType;

                        if (sectionType == null)
                        {
                            _logger.LogWarning("Section type '{Slug}' not found on block, skipping", section.Key);
                            continue;
                        }

                        var sectionTranslation = new BlockContentSectionTranslation
                        {
                            ContentId = blockContent.ContentId,
                            SectionTypeId = sectionType.SectionTypeId,
                            LanguageId = language.Id,
                            Content = section.Value
                        };

                        _context.BlockContentSectionTranslations.Add(sectionTranslation);
                    }
                }

                await _context.SaveChangesAsync();
            }

            _logger.LogInformation("Created block content {ContentId} with slug {Slug}", blockContent.ContentId, blockContent.Slug);

            // Return the created content
            return CreatedAtAction(nameof(GetBlockContent), new { id = blockContent.ContentId }, new
            {
                blockContent.ContentId,
                blockContent.Slug,
                blockContent.Name,
                blockContent.Description,
                Block = new { block.BlockId, block.Name, block.Slug },
                DefaultVariant = variant != null ? new { variant.VariantId, variant.Name, variant.Slug } : null,
                blockContent.AccessControl,
                blockContent.Visibility,
                blockContent.IsActive,
                blockContent.CreatedAt
            });
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
    [HttpPut("{id}")]
    public async Task<ActionResult<object>> UpdateBlockContent(Guid id, [FromBody] UpdateBlockContentRequest request)
    {
        try
        {
            var (effectiveTenantId, isAllTenantsMode) = GetTenantContext(null);

            var query = _context.BlockContents
                .Include(bc => bc.Block)
                    .ThenInclude(b => b!.BlockSections)
                    .ThenInclude(bs => bs.SectionType)
                .Include(bc => bc.SectionTranslations)
                .Where(bc => bc.ContentId == id);

            // Apply tenant filter if a tenantId is provided
            if (effectiveTenantId != null)
            {
                query = query.Where(bc => bc.TenantId == effectiveTenantId);
            }

            var blockContent = await query.FirstOrDefaultAsync();

            if (blockContent == null)
            {
                return NotFound(new { error = "Block content not found" });
            }

            // Update basic fields
            if (!string.IsNullOrEmpty(request.Slug) && request.Slug != blockContent.Slug)
            {
                var existingSlug = await _context.BlockContents
                    .AnyAsync(bc => bc.TenantId == blockContent.TenantId && bc.Slug == request.Slug && bc.ContentId != id);

                if (existingSlug)
                {
                    return BadRequest(new { error = $"Content with slug '{request.Slug}' already exists" });
                }
                blockContent.Slug = request.Slug;
            }

            if (!string.IsNullOrEmpty(request.Name)) blockContent.Name = request.Name;
            if (request.Description != null) blockContent.Description = request.Description;
            if (request.DefaultVariantId.HasValue) blockContent.DefaultVariantId = request.DefaultVariantId;
            if (!string.IsNullOrEmpty(request.AccessControl)) blockContent.AccessControl = request.AccessControl;
            if (!string.IsNullOrEmpty(request.Visibility)) blockContent.Visibility = request.Visibility;
            if (request.PublishAt.HasValue) blockContent.PublishAt = request.PublishAt;
            if (request.UnpublishAt.HasValue) blockContent.UnpublishAt = request.UnpublishAt;
            if (request.IsActive.HasValue) blockContent.IsActive = request.IsActive.Value;

            blockContent.UpdatedAt = DateTime.UtcNow;

            // Update translations if provided
            if (request.Translations != null)
            {
                foreach (var translation in request.Translations)
                {
                    var language = await _context.Languages
                        .FirstOrDefaultAsync(l => l.TenantId == blockContent.TenantId && l.LocaleCode == translation.LocaleCode);

                    if (language == null)
                    {
                        _logger.LogWarning("Language '{LocaleCode}' not found, skipping translations", translation.LocaleCode);
                        continue;
                    }

                    foreach (var section in translation.Sections)
                    {
                        var sectionType = blockContent.Block?.BlockSections
                            .FirstOrDefault(bs => bs.SectionType?.Slug == section.Key)?.SectionType;

                        if (sectionType == null)
                        {
                            _logger.LogWarning("Section type '{Slug}' not found on block, skipping", section.Key);
                            continue;
                        }

                        // Find existing translation or create new
                        var existingTranslation = blockContent.SectionTranslations
                            .FirstOrDefault(t => t.SectionTypeId == sectionType.SectionTypeId && t.LanguageId == language.Id);

                        if (existingTranslation != null)
                        {
                            existingTranslation.Content = section.Value;
                            existingTranslation.UpdatedAt = DateTime.UtcNow;
                        }
                        else
                        {
                            var newTranslation = new BlockContentSectionTranslation
                            {
                                ContentId = blockContent.ContentId,
                                SectionTypeId = sectionType.SectionTypeId,
                                LanguageId = language.Id,
                                Content = section.Value
                            };
                            _context.BlockContentSectionTranslations.Add(newTranslation);
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated block content {ContentId}", id);

            return Ok(new
            {
                blockContent.ContentId,
                blockContent.Slug,
                blockContent.Name,
                blockContent.Description,
                blockContent.AccessControl,
                blockContent.Visibility,
                blockContent.IsActive,
                blockContent.UpdatedAt
            });
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
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteBlockContent(Guid id)
    {
        try
        {
            var (effectiveTenantId, isAllTenantsMode) = GetTenantContext(null);

            var query = _context.BlockContents
                .Include(bc => bc.SectionTranslations)
                .Where(bc => bc.ContentId == id);

            // Apply tenant filter if a tenantId is provided
            if (effectiveTenantId != null)
            {
                query = query.Where(bc => bc.TenantId == effectiveTenantId);
            }

            var blockContent = await query.FirstOrDefaultAsync();

            if (blockContent == null)
            {
                return NotFound(new { error = "Block content not found" });
            }

            // Delete translations first (cascade should handle this, but be explicit)
            _context.BlockContentSectionTranslations.RemoveRange(blockContent.SectionTranslations);

            // Delete the content
            _context.BlockContents.Remove(blockContent);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted block content {ContentId}", id);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting block content {ContentId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get block content with all translations for editing
    /// </summary>
    [HttpGet("{id}/edit")]
    public async Task<ActionResult<object>> GetBlockContentForEdit(Guid id)
    {
        try
        {
            var (effectiveTenantId, isAllTenantsMode) = GetTenantContext(null);

            var query = _context.BlockContents
                .Include(bc => bc.Block)
                    .ThenInclude(b => b!.BlockSections)
                    .ThenInclude(bs => bs.SectionType)
                .Include(bc => bc.Block)
                    .ThenInclude(b => b!.Variants)
                .Include(bc => bc.DefaultVariant)
                .Include(bc => bc.SectionTranslations)
                    .ThenInclude(t => t.SectionType)
                .Include(bc => bc.SectionTranslations)
                    .ThenInclude(t => t.Language)
                .Where(bc => bc.ContentId == id);

            // Apply tenant filter if a tenantId is provided
            if (effectiveTenantId != null)
            {
                query = query.Where(bc => bc.TenantId == effectiveTenantId);
            }

            var content = await query.FirstOrDefaultAsync();

            if (content == null)
            {
                return NotFound(new { error = "Block content not found" });
            }

            // Get all languages for the tenant
            var languages = await _context.Languages
                .Where(l => l.TenantId == content.TenantId && l.IsActive)
                .OrderByDescending(l => l.IsDefault)
                .ThenBy(l => l.DisplayOrder)
                .Select(l => new { l.Id, l.LocaleCode, l.Name, l.IsDefault })
                .ToListAsync();

            // Build translations grouped by locale
            var translations = content.SectionTranslations
                .GroupBy(t => t.Language?.LocaleCode ?? "unknown")
                .ToDictionary(
                    g => g.Key,
                    g => g.ToDictionary(t => t.SectionType!.Slug, t => t.Content)
                );

            return Ok(new
            {
                content.ContentId,
                content.TenantId,
                content.Slug,
                content.Name,
                content.Description,
                content.AccessControl,
                content.Visibility,
                content.PublishAt,
                content.UnpublishAt,
                content.IsActive,
                Block = new
                {
                    content.Block!.BlockId,
                    content.Block.Name,
                    content.Block.Slug,
                    Sections = content.Block.BlockSections
                        .OrderBy(bs => bs.DisplayOrder)
                        .Select(bs => new
                        {
                            bs.SectionType!.SectionTypeId,
                            bs.SectionType.Slug,
                            bs.SectionType.Name,
                            bs.SectionType.FieldType,
                            bs.IsRequired,
                            bs.DefaultValue
                        }),
                    Variants = content.Block.Variants
                        .Where(v => v.IsActive)
                        .OrderBy(v => v.DisplayOrder)
                        .Select(v => new
                        {
                            v.VariantId,
                            v.Name,
                            v.Slug,
                            v.IsDefault
                        })
                },
                DefaultVariantId = content.DefaultVariantId,
                Languages = languages,
                Translations = translations,
                content.CreatedAt,
                content.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting block content for edit {ContentId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    // ==========================================
    // BLOCK MANAGEMENT ENDPOINTS
    // ==========================================

    /// <summary>
    /// Create a new block template
    /// </summary>
    [HttpPost("blocks")]
    public async Task<ActionResult<object>> CreateBlock([FromBody] CreateBlockRequest request)
    {
        try
        {
            // Check if slug already exists
            var existing = await _context.Blocks
                .FirstOrDefaultAsync(b => b.TenantId == request.TenantId && b.Slug == request.Slug);

            if (existing != null)
            {
                return BadRequest(new { error = $"Block with slug '{request.Slug}' already exists" });
            }

            var tenantId = request.TenantId ?? GetHeaderTenantId() ?? "tenant-test";
            var block = new Block
            {
                TenantId = tenantId,
                Name = request.Name,
                Slug = request.Slug,
                Description = request.Description,
                Icon = request.Icon,
                DisplayOrder = request.DisplayOrder ?? 0,
                IsActive = true
            };

            _context.Blocks.Add(block);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created block {BlockId} with slug {Slug}", block.BlockId, block.Slug);

            return CreatedAtAction(nameof(GetBlocks), new
            {
                block.BlockId,
                block.Name,
                block.Slug,
                block.Description,
                block.Icon,
                block.DisplayOrder,
                Sections = new List<object>(),
                Variants = new List<object>()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating block");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Update an existing block
    /// </summary>
    [HttpPut("blocks/{id}")]
    public async Task<ActionResult<object>> UpdateBlock(Guid id, [FromBody] UpdateBlockRequest request)
    {
        try
        {
            var (effectiveTenantId, isAllTenantsMode) = GetTenantContext(null);

            var query = _context.Blocks.Where(b => b.BlockId == id);

            // Apply tenant filter if a tenantId is provided
            if (effectiveTenantId != null)
            {
                query = query.Where(b => b.TenantId == effectiveTenantId);
            }

            var block = await query.FirstOrDefaultAsync();
            if (block == null)
            {
                return NotFound(new { error = "Block not found" });
            }

            // Check slug uniqueness if changed
            if (!string.IsNullOrEmpty(request.Slug) && request.Slug != block.Slug)
            {
                var existing = await _context.Blocks
                    .FirstOrDefaultAsync(b => b.TenantId == block.TenantId && b.Slug == request.Slug && b.BlockId != id);
                if (existing != null)
                {
                    return BadRequest(new { error = $"Block with slug '{request.Slug}' already exists" });
                }
                block.Slug = request.Slug;
            }

            if (!string.IsNullOrEmpty(request.Name)) block.Name = request.Name;
            if (request.Description != null) block.Description = request.Description;
            if (request.Icon != null) block.Icon = request.Icon;
            if (request.DisplayOrder.HasValue) block.DisplayOrder = request.DisplayOrder.Value;
            if (request.IsActive.HasValue) block.IsActive = request.IsActive.Value;

            block.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated block {BlockId}", id);

            return Ok(new
            {
                block.BlockId,
                block.Name,
                block.Slug,
                block.Description,
                block.Icon,
                block.DisplayOrder,
                block.IsActive
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating block {BlockId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Delete a block
    /// </summary>
    [HttpDelete("blocks/{id}")]
    public async Task<ActionResult> DeleteBlock(Guid id)
    {
        try
        {
            var (effectiveTenantId, isAllTenantsMode) = GetTenantContext(null);

            var query = _context.Blocks
                .Include(b => b.BlockContents)
                .Where(b => b.BlockId == id);

            // Apply tenant filter if a tenantId is provided
            if (effectiveTenantId != null)
            {
                query = query.Where(b => b.TenantId == effectiveTenantId);
            }

            var block = await query.FirstOrDefaultAsync();

            if (block == null)
            {
                return NotFound(new { error = "Block not found" });
            }

            // Check if block has content
            if (block.BlockContents.Any())
            {
                return BadRequest(new { error = $"Cannot delete block - it has {block.BlockContents.Count} content item(s). Delete content first or deactivate the block." });
            }

            _context.Blocks.Remove(block);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted block {BlockId}", id);

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
    [HttpPost("blocks/{blockId}/sections/{sectionTypeId}")]
    public async Task<ActionResult<object>> AddSectionToBlock(
        Guid blockId,
        Guid sectionTypeId,
        [FromBody] AddSectionRequest? request = null)
    {
        try
        {
            var block = await _context.Blocks.FindAsync(blockId);
            if (block == null)
            {
                return NotFound(new { error = "Block not found" });
            }

            var sectionType = await _context.SectionTypes.FindAsync(sectionTypeId);
            if (sectionType == null)
            {
                return NotFound(new { error = "Section type not found" });
            }

            // Check if already exists
            var existing = await _context.BlockSections
                .FirstOrDefaultAsync(bs => bs.BlockId == blockId && bs.SectionTypeId == sectionTypeId);

            if (existing != null)
            {
                return BadRequest(new { error = "Section type already added to this block" });
            }

            // Get max display order
            var maxOrder = await _context.BlockSections
                .Where(bs => bs.BlockId == blockId)
                .MaxAsync(bs => (int?)bs.DisplayOrder) ?? 0;

            var blockSection = new BlockSection
            {
                BlockId = blockId,
                SectionTypeId = sectionTypeId,
                IsRequired = request?.IsRequired ?? false,
                DisplayOrder = request?.DisplayOrder ?? maxOrder + 1,
                DefaultValue = request?.DefaultValue
            };

            _context.BlockSections.Add(blockSection);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Added section {SectionTypeId} to block {BlockId}", sectionTypeId, blockId);

            return Ok(new
            {
                blockSection.BlockId,
                blockSection.SectionTypeId,
                SectionType = new { sectionType.Slug, sectionType.Name, sectionType.FieldType },
                blockSection.IsRequired,
                blockSection.DisplayOrder
            });
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
    [HttpDelete("blocks/{blockId}/sections/{sectionTypeId}")]
    public async Task<ActionResult> RemoveSectionFromBlock(Guid blockId, Guid sectionTypeId)
    {
        try
        {
            var blockSection = await _context.BlockSections
                .FirstOrDefaultAsync(bs => bs.BlockId == blockId && bs.SectionTypeId == sectionTypeId);

            if (blockSection == null)
            {
                return NotFound(new { error = "Section not found on this block" });
            }

            _context.BlockSections.Remove(blockSection);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Removed section {SectionTypeId} from block {BlockId}", sectionTypeId, blockId);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing section from block");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Update section settings on a block (required, display order)
    /// </summary>
    [HttpPut("blocks/{blockId}/sections/{sectionTypeId}")]
    public async Task<ActionResult<object>> UpdateBlockSection(
        Guid blockId,
        Guid sectionTypeId,
        [FromBody] UpdateBlockSectionRequest request)
    {
        try
        {
            var blockSection = await _context.BlockSections
                .Include(bs => bs.SectionType)
                .FirstOrDefaultAsync(bs => bs.BlockId == blockId && bs.SectionTypeId == sectionTypeId);

            if (blockSection == null)
            {
                return NotFound(new { error = "Section not found on this block" });
            }

            if (request.IsRequired.HasValue) blockSection.IsRequired = request.IsRequired.Value;
            if (request.DisplayOrder.HasValue) blockSection.DisplayOrder = request.DisplayOrder.Value;
            if (request.DefaultValue != null) blockSection.DefaultValue = request.DefaultValue;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated section {SectionTypeId} on block {BlockId}", sectionTypeId, blockId);

            return Ok(new
            {
                blockSection.BlockId,
                blockSection.SectionTypeId,
                SectionType = new { blockSection.SectionType!.Slug, blockSection.SectionType.Name },
                blockSection.IsRequired,
                blockSection.DisplayOrder
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating block section");
            return BadRequest(new { error = ex.Message });
        }
    }

    // ==========================================
    // VARIANT MANAGEMENT ENDPOINTS
    // ==========================================

    /// <summary>
    /// Create a new variant for a block
    /// </summary>
    [HttpPost("blocks/{blockId}/variants")]
    public async Task<ActionResult<object>> CreateVariant(Guid blockId, [FromBody] CreateVariantRequest request)
    {
        try
        {
            var block = await _context.Blocks.FindAsync(blockId);
            if (block == null)
            {
                return NotFound(new { error = "Block not found" });
            }

            // Check slug uniqueness within block
            var existingSlug = await _context.Variants
                .AnyAsync(v => v.BlockId == blockId && v.Slug == request.Slug);

            if (existingSlug)
            {
                return BadRequest(new { error = $"Variant with slug '{request.Slug}' already exists on this block" });
            }

            // If this is being set as default, unset any existing default
            if (request.IsDefault)
            {
                var existingDefaults = await _context.Variants
                    .Where(v => v.BlockId == blockId && v.IsDefault)
                    .ToListAsync();

                foreach (var v in existingDefaults)
                {
                    v.IsDefault = false;
                }
            }

            // Get max display order
            var maxOrder = await _context.Variants
                .Where(v => v.BlockId == blockId)
                .MaxAsync(v => (int?)v.DisplayOrder) ?? 0;

            var variant = new Variant
            {
                TenantId = block.TenantId,
                BlockId = blockId,
                Name = request.Name,
                Slug = request.Slug,
                Description = request.Description,
                CssClass = request.CssClass,
                PreviewImageUrl = request.PreviewImageUrl,
                IsDefault = request.IsDefault,
                IsActive = true,
                DisplayOrder = request.DisplayOrder ?? maxOrder + 1
            };

            _context.Variants.Add(variant);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created variant {VariantId} with slug {Slug} for block {BlockId}", variant.VariantId, variant.Slug, blockId);

            return CreatedAtAction(nameof(GetBlockVariants), new { blockSlug = block.Slug }, new
            {
                variant.VariantId,
                variant.Name,
                variant.Slug,
                variant.Description,
                variant.CssClass,
                variant.PreviewImageUrl,
                variant.IsDefault,
                variant.IsActive,
                variant.DisplayOrder
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating variant");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Delete a variant from a block
    /// </summary>
    [HttpDelete("blocks/{blockId}/variants/{variantId}")]
    public async Task<ActionResult> DeleteVariant(Guid blockId, Guid variantId)
    {
        try
        {
            var variant = await _context.Variants
                .FirstOrDefaultAsync(v => v.VariantId == variantId && v.BlockId == blockId);

            if (variant == null)
            {
                return NotFound(new { error = "Variant not found on this block" });
            }

            // Check if any content is using this variant as default
            var contentUsingVariant = await _context.BlockContents
                .AnyAsync(bc => bc.DefaultVariantId == variantId);

            if (contentUsingVariant)
            {
                return BadRequest(new { error = "Cannot delete variant - it is being used as default by one or more content items" });
            }

            _context.Variants.Remove(variant);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted variant {VariantId} from block {BlockId}", variantId, blockId);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting variant {VariantId}", variantId);
            return BadRequest(new { error = ex.Message });
        }
    }

    // ==========================================
    // REQUEST MODELS
    // ==========================================

    public class CreateBlockRequest
    {
        public string? TenantId { get; set; }
        public required string Name { get; set; }
        public required string Slug { get; set; }
        public string? Description { get; set; }
        public string? Icon { get; set; }
        public int? DisplayOrder { get; set; }
    }

    public class UpdateBlockRequest
    {
        public string? Name { get; set; }
        public string? Slug { get; set; }
        public string? Description { get; set; }
        public string? Icon { get; set; }
        public int? DisplayOrder { get; set; }
        public bool? IsActive { get; set; }
    }

    public class AddSectionRequest
    {
        public bool IsRequired { get; set; }
        public int? DisplayOrder { get; set; }
        public string? DefaultValue { get; set; }
    }

    public class UpdateBlockSectionRequest
    {
        public bool? IsRequired { get; set; }
        public int? DisplayOrder { get; set; }
        public string? DefaultValue { get; set; }
    }

    public class CreateVariantRequest
    {
        public required string Name { get; set; }
        public required string Slug { get; set; }
        public string? Description { get; set; }
        public string? CssClass { get; set; }
        public string? PreviewImageUrl { get; set; }
        public bool IsDefault { get; set; }
        public int? DisplayOrder { get; set; }
    }

    public class CreateSectionTypeRequest
    {
        public string? TenantId { get; set; }
        public required string Name { get; set; }
        public required string Slug { get; set; }
        public string? Description { get; set; }
        public string? FieldType { get; set; }
        public string? ValidationRules { get; set; }
        public int? DisplayOrder { get; set; }
    }

    public class UpdateSectionTypeRequest
    {
        public string? Name { get; set; }
        public string? Slug { get; set; }
        public string? Description { get; set; }
        public string? FieldType { get; set; }
        public string? ValidationRules { get; set; }
        public int? DisplayOrder { get; set; }
        public bool? IsActive { get; set; }
    }

    // ==========================================
    // BLOCK CONTENT REQUEST MODELS
    // ==========================================

    public class CreateBlockContentRequest
    {
        public string? TenantId { get; set; }
        public required Guid BlockId { get; set; }
        public Guid? DefaultVariantId { get; set; }
        public required string Slug { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        public string? AccessControl { get; set; }
        public string? Visibility { get; set; }
        public DateTime? PublishAt { get; set; }
        public DateTime? UnpublishAt { get; set; }
        public bool? IsActive { get; set; }
        public List<TranslationInput>? Translations { get; set; }
    }

    public class UpdateBlockContentRequest
    {
        public string? Slug { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public Guid? DefaultVariantId { get; set; }
        public string? AccessControl { get; set; }
        public string? Visibility { get; set; }
        public DateTime? PublishAt { get; set; }
        public DateTime? UnpublishAt { get; set; }
        public bool? IsActive { get; set; }
        public List<TranslationInput>? Translations { get; set; }
    }

    public class TranslationInput
    {
        public required string LocaleCode { get; set; }
        public Dictionary<string, string?> Sections { get; set; } = new();
    }

    // Helper method to build content response
    private object BuildContentResponse(BlockContent content, Guid? languageId)
    {
        var sections = content.SectionTranslations
            .Where(t => !languageId.HasValue || t.LanguageId == languageId.Value)
            .GroupBy(t => t.Language?.LocaleCode ?? "unknown")
            .ToDictionary(
                g => g.Key,
                g => g.ToDictionary(t => t.SectionType!.Slug, t => t.Content)
            );

        return new
        {
            content.ContentId,
            content.TenantId,
            content.Slug,
            content.Name,
            content.Description,
            content.AccessControl,
            content.Visibility,
            content.PublishAt,
            content.UnpublishAt,
            content.IsActive,
            Block = new
            {
                content.Block!.BlockId,
                content.Block.Name,
                content.Block.Slug,
                Sections = content.Block.BlockSections
                    .OrderBy(bs => bs.DisplayOrder)
                    .Select(bs => new
                    {
                        bs.SectionType!.Slug,
                        bs.SectionType.Name,
                        bs.SectionType.FieldType,
                        bs.IsRequired
                    })
            },
            DefaultVariant = content.DefaultVariant != null ? new
            {
                content.DefaultVariant.VariantId,
                content.DefaultVariant.Name,
                content.DefaultVariant.Slug
            } : null,
            Translations = sections,
            content.CreatedAt,
            content.UpdatedAt
        };
    }
}
