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

    // Core entities
    public DbSet<Language> Languages { get; set; }

    // Block Structure (Schema-driven Content Blocks)
    public DbSet<Block> Blocks { get; set; }
    public DbSet<SectionType> SectionTypes { get; set; }
    public DbSet<BlockSection> BlockSections { get; set; }
    public DbSet<Variant> Variants { get; set; }
    public DbSet<BlockContent> BlockContents { get; set; }
    public DbSet<BlockContentSectionTranslation> BlockContentSectionTranslations { get; set; }

    // Block Categories and Tags
    public DbSet<BlockCategory> BlockCategories { get; set; }
    public DbSet<BlockCategoryTranslation> BlockCategoryTranslations { get; set; }
    public DbSet<BlockContentCategory> BlockContentCategories { get; set; }
    public DbSet<BlockTag> BlockTags { get; set; }
    public DbSet<BlockTagTranslation> BlockTagTranslations { get; set; }
    public DbSet<BlockContentTag> BlockContentTags { get; set; }

    // Media
    public DbSet<MediaFolder> MediaFolders { get; set; }
    public DbSet<Media> MediaItems { get; set; }
    public DbSet<MediaTranslation> MediaTranslations { get; set; }

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

        // Block configuration
        modelBuilder.Entity<Block>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Slug }).IsUnique();

            entity.HasMany(b => b.BlockSections)
                .WithOne(bs => bs.Block)
                .HasForeignKey(bs => bs.BlockId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(b => b.Variants)
                .WithOne(v => v.Block)
                .HasForeignKey(v => v.BlockId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(b => b.BlockContents)
                .WithOne(bc => bc.Block)
                .HasForeignKey(bc => bc.BlockId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // SectionType configuration
        modelBuilder.Entity<SectionType>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Slug }).IsUnique();

            entity.HasMany(st => st.BlockSections)
                .WithOne(bs => bs.SectionType)
                .HasForeignKey(bs => bs.SectionTypeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // BlockSection configuration (composite key)
        modelBuilder.Entity<BlockSection>(entity =>
        {
            entity.HasKey(e => new { e.BlockId, e.SectionTypeId });
        });

        // Variant configuration
        modelBuilder.Entity<Variant>(entity =>
        {
            entity.HasIndex(e => new { e.BlockId, e.Slug }).IsUnique();
        });

        // BlockContent configuration
        modelBuilder.Entity<BlockContent>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Slug }).IsUnique();

            entity.HasOne(bc => bc.DefaultVariant)
                .WithMany(v => v.BlockContents)
                .HasForeignKey(bc => bc.DefaultVariantId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(bc => bc.SectionTranslations)
                .WithOne(t => t.BlockContent)
                .HasForeignKey(t => t.ContentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(bc => bc.CategoryMappings)
                .WithOne(m => m.Content)
                .HasForeignKey(m => m.ContentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(bc => bc.TagMappings)
                .WithOne(m => m.Content)
                .HasForeignKey(m => m.ContentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // BlockContentSectionTranslation configuration
        modelBuilder.Entity<BlockContentSectionTranslation>(entity =>
        {
            entity.HasIndex(e => new { e.ContentId, e.SectionTypeId, e.LanguageId }).IsUnique();

            entity.HasOne(t => t.SectionType)
                .WithMany(st => st.Translations)
                .HasForeignKey(t => t.SectionTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.Language)
                .WithMany()
                .HasForeignKey(t => t.LanguageId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // BlockCategory configuration
        modelBuilder.Entity<BlockCategory>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Slug }).IsUnique();

            entity.HasOne(c => c.Parent)
                .WithMany(c => c.Children)
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(c => c.Translations)
                .WithOne(t => t.Category)
                .HasForeignKey(t => t.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(c => c.ContentMappings)
                .WithOne(m => m.Category)
                .HasForeignKey(m => m.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // BlockCategoryTranslation configuration
        modelBuilder.Entity<BlockCategoryTranslation>(entity =>
        {
            entity.HasIndex(e => new { e.CategoryId, e.LanguageId }).IsUnique();

            entity.HasOne(t => t.Language)
                .WithMany()
                .HasForeignKey(t => t.LanguageId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // BlockContentCategory configuration (composite key)
        modelBuilder.Entity<BlockContentCategory>(entity =>
        {
            entity.HasKey(e => new { e.ContentId, e.CategoryId });
        });

        // BlockTag configuration
        modelBuilder.Entity<BlockTag>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Slug }).IsUnique();

            entity.HasMany(t => t.Translations)
                .WithOne(tt => tt.Tag)
                .HasForeignKey(tt => tt.TagId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(t => t.ContentMappings)
                .WithOne(m => m.Tag)
                .HasForeignKey(m => m.TagId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // BlockTagTranslation configuration
        modelBuilder.Entity<BlockTagTranslation>(entity =>
        {
            entity.HasIndex(e => new { e.TagId, e.LanguageId }).IsUnique();

            entity.HasOne(t => t.Language)
                .WithMany()
                .HasForeignKey(t => t.LanguageId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // BlockContentTag configuration (composite key)
        modelBuilder.Entity<BlockContentTag>(entity =>
        {
            entity.HasKey(e => new { e.ContentId, e.TagId });
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

            entity.HasOne(i => i.BlockContent)
                .WithMany()
                .HasForeignKey(i => i.BlockContentId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(i => i.BlockCategory)
                .WithMany()
                .HasForeignKey(i => i.BlockCategoryId)
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
