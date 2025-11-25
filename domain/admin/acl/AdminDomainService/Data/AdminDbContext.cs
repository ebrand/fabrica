using AdminDomainService.Models;
using Microsoft.EntityFrameworkCore;
using Fabrica.Domain.Esb.Extensions;
using Fabrica.Domain.Esb.Models;

namespace AdminDomainService.Data;

public class AdminDbContext : DbContext
{
    public AdminDbContext(DbContextOptions<AdminDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<OutboxEvent> OutboxEvents { get; set; }
    public DbSet<OutboxConfig> OutboxConfigs { get; set; }

    // Cache tables for consuming events from other domains
    public DbSet<CacheEntry> CacheEntries { get; set; }
    public DbSet<CacheConfig> CacheConfigs { get; set; }

    // ESB domain registry - tracks all domains participating in the ESB
    public DbSet<EsbDomain> EsbDomains { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure outbox tables (for publishing events)
        modelBuilder.ConfigureOutbox();

        // Configure cache tables (for consuming events from other domains)
        modelBuilder.ConfigureCache();

        // Configure ESB domain registry
        modelBuilder.ConfigureEsbDomain();

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.StytchUserId);
            entity.HasIndex(e => e.IsActive);

            // Configure updated_at to be automatically set
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate();
        });

        // Configure Role entity
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId);
            entity.HasIndex(e => e.RoleName).IsUnique();
            entity.HasIndex(e => e.IsActive);

            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate();
        });

        // Configure Permission entity
        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasKey(e => e.PermissionId);
            entity.HasIndex(e => e.PermissionName).IsUnique();
            entity.HasIndex(e => e.Resource);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => new { e.Resource, e.Action }).IsUnique();

            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate();
        });

        // Configure RolePermission entity
        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(e => e.RolePermissionId);
            entity.HasIndex(e => e.RoleId);
            entity.HasIndex(e => e.PermissionId);
            entity.HasIndex(e => new { e.RoleId, e.PermissionId }).IsUnique();
        });

        // Configure UserRole entity
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => e.UserRoleId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.RoleId);
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => new { e.UserId, e.RoleId, e.TenantId }).IsUnique();

            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate();
        });
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => (e.Entity is User || e.Entity is Role || e.Entity is Permission || e.Entity is UserRole)
                     && (e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            if (entry.Entity is User user)
                user.UpdatedAt = DateTime.UtcNow;
            else if (entry.Entity is Role role)
                role.UpdatedAt = DateTime.UtcNow;
            else if (entry.Entity is Permission permission)
                permission.UpdatedAt = DateTime.UtcNow;
            else if (entry.Entity is UserRole userRole)
                userRole.UpdatedAt = DateTime.UtcNow;
        }
    }
}
