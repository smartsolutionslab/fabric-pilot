using Microsoft.Extensions.Configuration;

namespace FabricPilot.Modules.Identity.Infrastructure.Keycloak;

/// <summary>
/// Where the realm's admin API is and the two confidential clients that call it, from <c>KeycloakAdmin:*</c>: the user
/// admin, which manages a tenant's users, and the staff reader, which only lists the staff (docs/adr/0053). The AppHost
/// provides them; the secrets are its parameters <c>keycloak-user-admin-secret</c> and
/// <c>keycloak-staff-reader-secret</c>, never a settings file.
/// </summary>
public sealed record KeycloakAdminSettings(Uri BaseAddress, string Realm, KeycloakClientCredentials UserAdmin, KeycloakClientCredentials StaffReader)
{
    private const string Section = "KeycloakAdmin";

    /// <summary>The realm's admin API, ending in a slash so relative paths resolve beneath it.</summary>
    public Uri AdminApi => new(BaseAddress, $"admin/realms/{Uri.EscapeDataString(Realm)}/");

    public Uri TokenEndpoint => new(BaseAddress, $"realms/{Uri.EscapeDataString(Realm)}/protocol/openid-connect/token");

    public static KeycloakAdminSettings From(IConfiguration configuration)
    {
        Ensure.That(configuration).IsNotNull();

        var baseAddress = Required(configuration, "BaseAddress");
        return new KeycloakAdminSettings(
            new Uri(baseAddress.EndsWith('/') ? baseAddress : baseAddress + "/", UriKind.Absolute),
            Required(configuration, "Realm"),
            new KeycloakClientCredentials(Required(configuration, "ClientId"), Required(configuration, "ClientSecret")),
            new KeycloakClientCredentials(Required(configuration, "StaffReaderClientId"), Required(configuration, "StaffReaderClientSecret")));
    }

    private static string Required(IConfiguration configuration, string key) =>
        configuration[$"{Section}:{key}"] is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException($"{Section}:{key} is missing; the AppHost provides it.");
}
