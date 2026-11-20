using FabricPilot.Contracts.Identity;
using FabricPilot.Modules.Identity.Application;
using FabricPilot.Modules.Identity.Infrastructure.Keycloak;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class IdentityKeycloak
{
    private const string UserAdmin = "user-admin";

    private const string StaffReader = "staff-reader";

    /// <summary>
    /// The realm's users through Keycloak's admin API, as the confidential clients in <c>KeycloakAdmin:*</c>, each with
    /// its own token: the user admin manages a tenant's users, the staff reader lists the staff for other modules
    /// (<see cref="IStaffDirectory"/>). Keycloak is the module's only store: it has no table.
    /// </summary>
    public static IServiceCollection AddIdentityKeycloak(this IServiceCollection services, IConfiguration configuration)
    {
        Ensure.That(configuration).IsNotNull();

        services
            .AddSingleton(_ => KeycloakAdminSettings.From(configuration))
            .AddKeyedSingleton(UserAdmin, (provider, _) => TokenFor(provider, provider.GetRequiredService<KeycloakAdminSettings>().UserAdmin))
            .AddKeyedSingleton(StaffReader, (provider, _) => TokenFor(provider, provider.GetRequiredService<KeycloakAdminSettings>().StaffReader));

        // A client_credentials mint changes nothing on the server: a second request mints a second token, which simply
        // supersedes the first, so retrying the POST cannot apply anything twice.
        services.AddHttpClient(KeycloakAdminToken.ClientName)
            .RetryEveryMethod();

        // The admin API's POSTs (creating a user, granting a role) keep the default: one attempt.
        services.AddHttpClient<ITenantUserDirectory, KeycloakTenantUserDirectory>((provider, client) =>
                client.BaseAddress = provider.GetRequiredService<KeycloakAdminSettings>().AdminApi)
            .AddHttpMessageHandler(provider => new KeycloakAdminAuthentication(provider.GetRequiredKeyedService<KeycloakAdminToken>(UserAdmin)));

        // The staff reader only reads (GET), with a token of its own.
        services.AddHttpClient<IStaffDirectory, KeycloakStaffDirectory>((provider, client) =>
                client.BaseAddress = provider.GetRequiredService<KeycloakAdminSettings>().AdminApi)
            .AddHttpMessageHandler(provider => new KeycloakAdminAuthentication(provider.GetRequiredKeyedService<KeycloakAdminToken>(StaffReader)));

        return services;
    }

    private static KeycloakAdminToken TokenFor(IServiceProvider provider, KeycloakClientCredentials credentials) =>
        new(provider.GetRequiredService<IHttpClientFactory>(), provider.GetRequiredService<KeycloakAdminSettings>(), credentials, provider.GetRequiredService<TimeProvider>());
}
