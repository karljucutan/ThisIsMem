using Microsoft.EntityFrameworkCore;

namespace API.Infrastructure.Persistence;

public static class MemDbContextExtensions
{
    public static IServiceCollection AddRagDbContext(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(MemDbContext.ConnectionStringName);

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException($"ConnectionStrings:{MemDbContext.ConnectionStringName} is not set.");

        services.AddDbContext<MemDbContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions => npgsqlOptions.UseVector()));

        return services;
    }

    public static async Task EnsureMemDbCreatedAsync(this IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MemDbContext>();

        await dbContext.Database.EnsureCreatedAsync(cancellationToken);
    }
}