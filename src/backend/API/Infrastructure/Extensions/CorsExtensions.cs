namespace API.Infrastructure.Extensions;

public static class CorsExtensions
{
    public static IServiceCollection AddAppCors(this IServiceCollection services, IConfiguration configuration)
    {
        var policyName = configuration["Cors:PolicyName"] ?? "FrontendCors";
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        services.AddCors(options =>
        {
            options.AddPolicy(policyName, policy =>
            {
                if (allowedOrigins.Length == 0)
                    throw new InvalidOperationException("Cors:AllowedOrigins must contain at least one origin.");

                policy.WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        return services;
    }

    public static WebApplication UseAppCors(this WebApplication app)
    {
        var policyName = app.Configuration["Cors:PolicyName"] ?? "FrontendCors";

        app.UseCors(policyName);

        return app;
    }
}