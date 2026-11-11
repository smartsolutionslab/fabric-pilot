using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Microsoft.Extensions.DependencyInjection;

public static class Authentication
{
    /// <summary>JWT bearer tokens issued by the Keycloak realm at Authentication:Authority, for the API audience.</summary>
    public static IServiceCollection AddKeycloakAuthentication(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        Ensure.That(configuration).IsNotNull();
        Ensure.That(environment).IsNotNull();

        var authority = configuration["Authentication:Authority"]
            ?? throw new InvalidOperationException("Authentication:Authority is missing; the AppHost provides it in development.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authority;
                options.Audience = "fabric-pilot-api";
                options.RequireHttpsMetadata = !environment.IsDevelopment();
            });
        services.AddAuthorization();

        return services;
    }
}
