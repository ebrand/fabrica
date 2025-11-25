using Microsoft.EntityFrameworkCore;
using ProductDomainService.Models;
using Fabrica.Domain.Esb.Extensions;
using Fabrica.Domain.Esb.Models;

namespace ProductDomainService.Data;

public class ProductDbContext : DbContext
{
    public ProductDbContext(DbContextOptions<ProductDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products { get; set; }
    public DbSet<ProductVariant> ProductVariants { get; set; }
    public DbSet<ProductImage> ProductImages { get; set; }
    public DbSet<ProductTag> ProductTags { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<ProductCategory> ProductCategories { get; set; }
    public DbSet<Inventory> Inventory { get; set; }
    public DbSet<OutboxEvent> OutboxEvents { get; set; }
    public DbSet<OutboxConfig> OutboxConfigs { get; set; }

    // Cache tables for consuming events from other domains
    public DbSet<CacheEntry> CacheEntries { get; set; }
    public DbSet<CacheConfig> CacheConfigs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure outbox tables (for publishing events)
        modelBuilder.ConfigureOutbox();

        // Configure cache tables (for consuming events from other domains)
        modelBuilder.ConfigureCache();

        // Product configuration
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("product", "fabrica");
            entity.HasIndex(e => new { e.TenantId, e.Sku }).IsUnique();
            entity.HasIndex(e => new { e.TenantId, e.Slug });

            entity.HasMany(p => p.Images)
                .WithOne(i => i.Product)
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(p => p.Variants)
                .WithOne(v => v.Product)
                .HasForeignKey(v => v.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(p => p.Tags)
                .WithOne(t => t.Product)
                .HasForeignKey(t => t.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(p => p.ProductCategories)
                .WithOne(pc => pc.Product)
                .HasForeignKey(pc => pc.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ProductVariant configuration
        modelBuilder.Entity<ProductVariant>(entity =>
        {
            entity.ToTable("product_variant", "fabrica");
            entity.HasIndex(e => new { e.ProductId, e.Sku }).IsUnique();
        });

        // ProductImage configuration
        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.ToTable("product_image", "fabrica");
            entity.HasIndex(e => new { e.ProductId, e.DisplayOrder });
        });

        // ProductTag configuration
        modelBuilder.Entity<ProductTag>(entity =>
        {
            entity.ToTable("product_tag", "fabrica");
            entity.HasIndex(e => new { e.ProductId, e.Tag });
        });

        // Category configuration
        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("category", "fabrica");
            entity.HasIndex(e => new { e.TenantId, e.Slug }).IsUnique();

            entity.HasOne(c => c.ParentCategory)
                .WithMany(c => c.ChildCategories)
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(c => c.ProductCategories)
                .WithOne(pc => pc.Category)
                .HasForeignKey(pc => pc.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ProductCategory configuration
        modelBuilder.Entity<ProductCategory>(entity =>
        {
            entity.ToTable("product_category", "fabrica");
            entity.HasIndex(e => new { e.ProductId, e.CategoryId }).IsUnique();
        });

        // Inventory configuration
        modelBuilder.Entity<Inventory>(entity =>
        {
            entity.ToTable("inventory", "fabrica");
            entity.HasIndex(e => new { e.TenantId, e.ProductId, e.LocationId });
            entity.HasIndex(e => new { e.TenantId, e.ProductVariantId, e.LocationId });
        });
    }
}
