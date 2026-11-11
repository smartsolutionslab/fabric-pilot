using FabricPilot.Host;
using Microsoft.AspNetCore.Authentication;

namespace Microsoft.Extensions.DependencyInjection;

public static class RoleClaims
{
    /// <summary>Keycloak's realm roles and the API client's roles become <c>ClaimTypes.Role</c> claims, which <c>RequireRole</c> reads.</summary>
    public static IServiceCollection AddKeycloakRoles(this IServiceCollection services)
    {
        Ensure.That(services).IsNotNull();

        return services.AddTransient<IClaimsTransformation, KeycloakRoleClaims>();
    }
}
