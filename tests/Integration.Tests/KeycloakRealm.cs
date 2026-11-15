using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace FabricPilot.Integration.Tests;

/// <summary>
/// Reads the realm back as its master administrator, never through the API under test: what Keycloak holds for a user,
/// and which admin roles the identity module's service account has.
/// </summary>
public sealed class KeycloakRealm(AspireFixture aspire)
{
    private const string Realm = "/admin/realms/fabric-pilot";

    public async Task<HttpClient> AdminAsync(CancellationToken cancellationToken)
    {
        var identity = new HttpClient { BaseAddress = aspire.App.GetEndpoint("keycloak", "http") };
        var password = aspire.App.Services.GetRequiredService<DistributedApplicationModel>().Resources.OfType<ParameterResource>().Single(parameter => parameter.Name == "keycloak-password");
        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = "admin-cli",
            ["username"] = "admin",
            ["password"] = await password.GetValueAsync(cancellationToken) ?? string.Empty,
        });
        using var response = await identity.PostAsync(new Uri("/realms/master/protocol/openid-connect/token", UriKind.Relative), form, cancellationToken);
        response.EnsureSuccessStatusCode();
        var token = await response.Content.ReadFromJsonAsync<Token>(cancellationToken)
            ?? throw new InvalidOperationException("The master realm returned no token.");
        identity.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        return identity;
    }

    /// <summary>The user with this username, with their groups and the realm roles they hold, directly or through a composite; null when there is none.</summary>
    public async Task<RealmUser?> FindAsync(string username, CancellationToken cancellationToken)
    {
        using var admin = await AdminAsync(cancellationToken);
        var found = await admin.GetFromJsonAsync<UserRepresentation[]>(new Uri($"{Realm}/users?username={Uri.EscapeDataString(username)}&exact=true", UriKind.Relative), cancellationToken) ?? [];
        if (found is not [var user])
        {
            return null;
        }

        var groups = await admin.GetFromJsonAsync<Named[]>(new Uri($"{Realm}/users/{user.Id}/groups", UriKind.Relative), cancellationToken) ?? [];
        var mapped = await admin.GetFromJsonAsync<Named[]>(new Uri($"{Realm}/users/{user.Id}/role-mappings/realm", UriKind.Relative), cancellationToken) ?? [];
        var effective = await admin.GetFromJsonAsync<Named[]>(new Uri($"{Realm}/users/{user.Id}/role-mappings/realm/composite", UriKind.Relative), cancellationToken) ?? [];
        return new RealmUser(
            Guid.Parse(user.Id),
            user.Username,
            user.Email,
            [.. groups.Select(group => group.Path!)],
            [.. mapped.Select(role => role.Name!)],
            [.. effective.Select(role => role.Name!)],
            user.RequiredActions ?? []);
    }

    public async Task<Guid> IdOfAsync(string username, CancellationToken cancellationToken) =>
        (await FindAsync(username, cancellationToken))?.Id ?? throw new InvalidOperationException($"The realm has no user {username}.");

    /// <summary>The realm-management roles the identity module's service account holds, composites expanded.</summary>
    public async Task<IReadOnlyList<string>> ServiceAccountAdminRolesAsync(CancellationToken cancellationToken)
    {
        using var admin = await AdminAsync(cancellationToken);
        var serviceAccount = await IdOfAsync("service-account-fabric-pilot-user-admin", cancellationToken);
        var clients = await admin.GetFromJsonAsync<Client[]>(new Uri($"{Realm}/clients?clientId=realm-management", UriKind.Relative), cancellationToken) ?? [];
        var roles = await admin.GetFromJsonAsync<Named[]>(new Uri($"{Realm}/users/{serviceAccount}/role-mappings/clients/{clients.Single().Id}/composite", UriKind.Relative), cancellationToken) ?? [];
        return [.. roles.Select(role => role.Name!).Order(StringComparer.Ordinal)];
    }

    public sealed record RealmUser(
        Guid Id,
        string Username,
        string? Email,
        IReadOnlyList<string> Groups,
        IReadOnlyList<string> MappedRoles,
        IReadOnlyList<string> EffectiveRoles,
        IReadOnlyList<string> RequiredActions);

    private sealed record Token([property: JsonPropertyName("access_token")] string AccessToken);

    private sealed record UserRepresentation(string Id, string Username, string? Email, IReadOnlyList<string>? RequiredActions);

    private sealed record Named(string? Name, string? Path);

    private sealed record Client(string Id);
}
