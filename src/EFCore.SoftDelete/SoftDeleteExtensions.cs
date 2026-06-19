using Microsoft.EntityFrameworkCore;

namespace EFCore.SoftDelete;

public static class SoftDeleteExtensions
{
    public static IQueryable<T> OnlyDeleted<T>(this DbSet<T> dbSet) where T : class, ISoftDeletable
        => dbSet.IgnoreQueryFilters().Where(e => e.IsDeleted);

    public static void Restore(this ISoftDeletable entity)
    {
        entity.IsDeleted = false;
        entity.DeletedAt = null;
    }
}
