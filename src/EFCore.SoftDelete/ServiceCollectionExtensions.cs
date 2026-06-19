using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EFCore.SoftDelete;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSoftDeleteDbContext<TContext>(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> optionsAction)
        where TContext : SoftDeleteDbContext
    {
        services.AddDbContext<TContext>(optionsAction);
        return services;
    }
}
