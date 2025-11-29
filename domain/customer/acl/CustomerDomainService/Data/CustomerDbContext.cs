using Microsoft.EntityFrameworkCore;
using CustomerDomainService.Models;
using Fabrica.Domain.Esb.Extensions;
using Fabrica.Domain.Esb.Models;

namespace CustomerDomainService.Data;

public class CustomerDbContext : DbContext
{
    public CustomerDbContext(DbContextOptions<CustomerDbContext> options)
        : base(options)
    {
    }

    public DbSet<Customer> Customers { get; set; }
    public DbSet<CustomerAddress> CustomerAddresses { get; set; }
    public DbSet<CustomerNote> CustomerNotes { get; set; }
    public DbSet<CustomerSegment> CustomerSegments { get; set; }
    public DbSet<CustomerSegmentMember> CustomerSegmentMembers { get; set; }
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

        // Customer configuration
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("customer", "fabrica");
            entity.HasIndex(e => new { e.TenantId, e.Email }).IsUnique();
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => new { e.LastName, e.FirstName });

            entity.HasMany(c => c.Addresses)
                .WithOne(a => a.Customer)
                .HasForeignKey(a => a.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(c => c.Notes_Collection)
                .WithOne(n => n.Customer)
                .HasForeignKey(n => n.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(c => c.SegmentMemberships)
                .WithOne(m => m.Customer)
                .HasForeignKey(m => m.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // CustomerAddress configuration
        modelBuilder.Entity<CustomerAddress>(entity =>
        {
            entity.ToTable("customer_address", "fabrica");
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => new { e.CustomerId, e.IsDefault });
        });

        // CustomerNote configuration
        modelBuilder.Entity<CustomerNote>(entity =>
        {
            entity.ToTable("customer_note", "fabrica");
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.NoteType);
        });

        // CustomerSegment configuration
        modelBuilder.Entity<CustomerSegment>(entity =>
        {
            entity.ToTable("customer_segment", "fabrica");
            entity.HasIndex(e => new { e.TenantId, e.Slug }).IsUnique();
            entity.HasIndex(e => e.TenantId);

            entity.HasMany(s => s.Members)
                .WithOne(m => m.Segment)
                .HasForeignKey(m => m.SegmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // CustomerSegmentMember configuration (composite key)
        modelBuilder.Entity<CustomerSegmentMember>(entity =>
        {
            entity.ToTable("customer_segment_member", "fabrica");
            entity.HasKey(e => new { e.CustomerId, e.SegmentId });
        });
    }
}
