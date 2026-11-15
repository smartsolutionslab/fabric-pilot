using FabricPilot.Modules.Identity.Application;
using FabricPilot.Modules.Identity.Infrastructure.Keycloak;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class IdentityKeycloak
{
    /// <summary>
    /// The realm's users through Keycloak's admin API, as the confidential client in <c>KeycloakAdmin:*</c>. Keycloak is
    /// the module's only store: it has no table.
    /// </summary>
    public static IServiceCollection AddIdentityKeycloak(this IServiceCollection services, IConfiguration configuration)
    {
        Ensure.That(configuration).IsNotNull();

        services
            .AddSingleton(_ => KeycloakAdminSettings.From(configuration))
            .AddSingleton<KeycloakAdminToken>()
            .AddTransient<KeycloakAdminAuthentication>();

        // A client_credentials mint changes nothing on the server: a second request mints a second token, which simply
        // supersedes the first, so retrying the POST cannot apply anything twice.
        services.AddHttpClient(KeycloakAdminToken.ClientName)
            .RetryEveryMethod();

        // The admin API's POSTs (creating a user, granting a role) keep the default: one attempt.
        services.AddHttpClient<ITenantUserDirectory, KeycloakTenantUserDirectory>((provider, client) =>
                client.BaseAddress = provider.GetRequiredService<KeycloakAdminSettings>().AdminApi)
            .AddHttpMessageHandler<KeycloakAdminAuthentication>();

        return services;
    }
}
