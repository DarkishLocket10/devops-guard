using DevOpsGuard.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevOpsGuard.Infrastructure.Data;

public class DevOpsGuardDbContext : DbContext
{
    public DevOpsGuardDbContext(DbContextOptions<DevOpsGuardDbContext> options) : base(options) { }

    public DbSet<WorkItem> WorkItems => Set<WorkItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DevOpsGuardDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<WorkItem>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Property(e => e.CreatedAtUtc).CurrentValue = utcNow;
                entry.Property(e => e.UpdatedAtUtc).CurrentValue = utcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(e => e.UpdatedAtUtc).CurrentValue = utcNow;
            }
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
