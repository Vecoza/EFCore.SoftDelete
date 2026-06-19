using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace EFCore.SoftDelete;

public abstract class SoftDeleteDbContext : DbContext
{
    protected SoftDeleteDbContext(DbContextOptions options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
                continue;

            var param = Expression.Parameter(entityType.ClrType, "e");
            var property = Expression.Property(param, nameof(ISoftDeletable.IsDeleted));
            var notDeleted = Expression.Not(property);
            var lambda = Expression.Lambda(notDeleted, param);

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
        }
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        InterceptSoftDeletes();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        InterceptSoftDeletes();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    public IQueryable<T> IncludeDeleted<T>() where T : class, ISoftDeletable
        => Set<T>().IgnoreQueryFilters();

    private void InterceptSoftDeletes()
    {
        var entries = ChangeTracker
            .Entries<ISoftDeletable>()
            .Where(e => e.State == EntityState.Deleted)
            .ToList();

        foreach (var entry in entries)
        {
            entry.State = EntityState.Modified;
            entry.Entity.IsDeleted = true;
            entry.Entity.DeletedAt = DateTime.UtcNow;
        }
    }
}
