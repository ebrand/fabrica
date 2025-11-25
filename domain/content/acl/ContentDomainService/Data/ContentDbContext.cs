using Microsoft.EntityFrameworkCore;
using ContentDomainService.Models;
using Fabrica.Domain.Esb.Extensions;
using Fabrica.Domain.Esb.Models;

namespace ContentDomainService.Data;

public class ContentDbContext : DbContext
{
    public ContentDbContext(DbContextOptions<ContentDbContext> options)
        : base(options)
    {
    }

    // Core content entities
    public DbSet<Language> Languages { get; set; }
    public DbSet<ContentType> ContentTypes { get; set; }
    public DbSet<Content> Contents { get; set; }
    public DbSet<ContentTranslation> ContentTranslations { get; set; }

    // Categories and tags
    public DbSet<ContentCategory> ContentCategories { get; set; }
    public DbSet<ContentCategoryTranslation> ContentCategoryTranslations { get; set; }
    public DbSet<ContentCategoryMapping> ContentCategoryMappings { get; set; }
    public DbSet<ContentTag> ContentTags { get; set; }
    public DbSet<ContentTagTranslation> ContentTagTranslations { get; set; }
    public DbSet<ContentTagMapping> ContentTagMappings { get; set; }

    // Media
    public DbSet<MediaFolder> MediaFolders { get; set; }
    public DbSet<Media> MediaItems { get; set; }
    public DbSet<MediaTranslation> MediaTranslations { get; set; }

    // Content Blocks
    public DbSet<ContentBlock> ContentBlocks { get; set; }
    public DbSet<ContentBlockTranslation> ContentBlockTranslations { get; set; }

    // Menus
    public DbSet<Menu> Menus { get; set; }
    public DbSet<MenuItem> MenuItems { get; set; }
    public DbSet<MenuItemTranslation> MenuItemTranslations { get; set; }

    // ESB support
    public DbSet<OutboxEvent> OutboxEvents { get; set; }
    public DbSet<OutboxConfig> OutboxConfigs { get; set; }
    public DbSet<CacheEntry> CacheEntries { get; set; }
    public DbSet<CacheConfig> CacheConfigs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure outbox tables (for publishing events)
        modelBuilder.ConfigureOutbox();

        // Configure cache tables (for consuming events from other domains)
        modelBuilder.ConfigureCache();

        // Language configuration
        modelBuilder.Entity<Language>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.LocaleCode }).IsUnique();
        });

        // ContentType configuration
        modelBuilder.Entity<ContentType>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Code }).IsUnique();
            entity.HasMany(ct => ct.Contents)
                .WithOne(c => c.ContentType)
                .HasForeignKey(c => c.ContentTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Content configuration
        modelBuilder.Entity<Content>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Slug }).IsUnique();
            entity.HasIndex(e => e.ContentTypeId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.ParentId);

            entity.HasOne(c => c.Parent)
                .WithMany(c => c.Children)
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(c => c.Translations)
                .WithOne(t => t.Content)
                .HasForeignKey(t => t.ContentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(c => c.FeaturedImage)
                .WithMany()
                .HasForeignKey(c => c.FeaturedImageId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ContentTranslation configuration
        modelBuilder.Entity<ContentTranslation>(entity =>
        {
            entity.HasIndex(e => new { e.ContentId, e.LocaleCode }).IsUnique();
        });

        // ContentCategory configuration
        modelBuilder.Entity<ContentCategory>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Slug }).IsUnique();

            entity.HasOne(c => c.Parent)
                .WithMany(c => c.Children)
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(c => c.Translations)
                .WithOne(t => t.ContentCategory)
                .HasForeignKey(t => t.ContentCategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ContentCategoryTranslation configuration
        modelBuilder.Entity<ContentCategoryTranslation>(entity =>
        {
            entity.HasIndex(e => new { e.ContentCategoryId, e.LocaleCode }).IsUnique();
        });

        // ContentCategoryMapping configuration (composite key)
        modelBuilder.Entity<ContentCategoryMapping>(entity =>
        {
            entity.HasKey(e => new { e.ContentId, e.ContentCategoryId });

            entity.HasOne(m => m.Content)
                .WithMany(c => c.CategoryMappings)
                .HasForeignKey(m => m.ContentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(m => m.ContentCategory)
                .WithMany(c => c.ContentMappings)
                .HasForeignKey(m => m.ContentCategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ContentTag configuration
        modelBuilder.Entity<ContentTag>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Slug }).IsUnique();

            entity.HasMany(t => t.Translations)
                .WithOne(tt => tt.ContentTag)
                .HasForeignKey(tt => tt.ContentTagId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ContentTagTranslation configuration
        modelBuilder.Entity<ContentTagTranslation>(entity =>
        {
            entity.HasIndex(e => new { e.ContentTagId, e.LocaleCode }).IsUnique();
        });

        // ContentTagMapping configuration (composite key)
        modelBuilder.Entity<ContentTagMapping>(entity =>
        {
            entity.HasKey(e => new { e.ContentId, e.ContentTagId });

            entity.HasOne(m => m.Content)
                .WithMany(c => c.TagMappings)
                .HasForeignKey(m => m.ContentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(m => m.ContentTag)
                .WithMany(t => t.ContentMappings)
                .HasForeignKey(m => m.ContentTagId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // MediaFolder configuration
        modelBuilder.Entity<MediaFolder>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.ParentId, e.Slug }).IsUnique();

            entity.HasOne(f => f.Parent)
                .WithMany(f => f.Children)
                .HasForeignKey(f => f.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(f => f.Media)
                .WithOne(m => m.Folder)
                .HasForeignKey(m => m.FolderId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Media configuration
        modelBuilder.Entity<Media>(entity =>
        {
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.FolderId);
            entity.HasIndex(e => e.MediaType);

            entity.HasMany(m => m.Translations)
                .WithOne(t => t.Media)
                .HasForeignKey(t => t.MediaId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // MediaTranslation configuration
        modelBuilder.Entity<MediaTranslation>(entity =>
        {
            entity.HasIndex(e => new { e.MediaId, e.LocaleCode }).IsUnique();
        });

        // ContentBlock configuration
        modelBuilder.Entity<ContentBlock>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Code }).IsUnique();

            entity.HasMany(b => b.Translations)
                .WithOne(t => t.ContentBlock)
                .HasForeignKey(t => t.ContentBlockId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ContentBlockTranslation configuration
        modelBuilder.Entity<ContentBlockTranslation>(entity =>
        {
            entity.HasIndex(e => new { e.ContentBlockId, e.LocaleCode }).IsUnique();
        });

        // Menu configuration
        modelBuilder.Entity<Menu>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Code }).IsUnique();

            entity.HasMany(m => m.Items)
                .WithOne(i => i.Menu)
                .HasForeignKey(i => i.MenuId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // MenuItem configuration
        modelBuilder.Entity<MenuItem>(entity =>
        {
            entity.HasIndex(e => e.MenuId);
            entity.HasIndex(e => e.ParentId);

            entity.HasOne(i => i.Parent)
                .WithMany(i => i.Children)
                .HasForeignKey(i => i.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(i => i.Content)
                .WithMany()
                .HasForeignKey(i => i.ContentId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(i => i.Category)
                .WithMany()
                .HasForeignKey(i => i.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(i => i.Translations)
                .WithOne(t => t.MenuItem)
                .HasForeignKey(t => t.MenuItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // MenuItemTranslation configuration
        modelBuilder.Entity<MenuItemTranslation>(entity =>
        {
            entity.HasIndex(e => new { e.MenuItemId, e.LocaleCode }).IsUnique();
        });
    }
}
