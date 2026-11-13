using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Aspire.Hosting.Testing;

namespace FabricPilot.Integration.Tests;

/// <summary>
/// The identity module's confidential client, fabric-pilot-user-admin, holds the least the admin API lets it work with.
/// A spike against Keycloak 26.6 found that manage-users alone permits every call the module makes (the group lookup,
/// creating a user in a group, reading their groups and roles, mapping and unmapping realm roles, the members list, the
/// invitation and the delete), so view-users and query-groups, which the design proposed, are not granted. Every
/// identity integration test then runs on that one role.
/// </summary>
public class IdentityUserAdminClientTests(AspireFixture aspire)
{
    private const string Realm = "/admin/realms/fabric-pilot";

    private static readonly string[] TenantAGroups = ["/tenants/tenant-a"];

    private readonly KeycloakRealm realm = new(aspire);

    [Fact]
    public async Task The_user_admin_service_account_holds_manage_users_and_no_other_realm_management_role()
    {
        var roles = await realm.ServiceAccountAdminRolesAsync(TestContext.Current.CancellationToken);

        roles.ShouldBe(["manage-users"]);
    }

    [Fact]
    public async Task Without_manage_users_the_admin_api_refuses_to_create_a_user()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var admin = await realm.AdminAsync(cancellationToken);
        var clientId = $"spike-{Guid.CreateVersion7():N}";
        using var created = await admin.PostAsJsonAsync(new Uri($"{Realm}/clients", UriKind.Relative), new { clientId, publicClient = false, secret = "spike", serviceAccountsEnabled = true, standardFlowEnabled = false }, cancellationToken);
        created.StatusCode.ShouldBe(HttpStatusCode.Created);
        var client = (await admin.GetFromJsonAsync<Client[]>(new Uri($"{Realm}/clients?clientId={clientId}", UriKind.Relative), cancellationToken))!.Single();
        try
        {
            var serviceAccount = await admin.GetFromJsonAsync<Client>(new Uri($"{Realm}/clients/{client.Id}/service-account-user", UriKind.Relative), cancellationToken);
            var realmManagement = (await admin.GetFromJsonAsync<Client[]>(new Uri($"{Realm}/clients?clientId=realm-management", UriKind.Relative), cancellationToken))!.Single();
            var viewUsers = await admin.GetFromJsonAsync<Role>(new Uri($"{Realm}/clients/{realmManagement.Id}/roles/view-users", UriKind.Relative), cancellationToken);
            var queryGroups = await admin.GetFromJsonAsync<Role>(new Uri($"{Realm}/clients/{realmManagement.Id}/roles/query-groups", UriKind.Relative), cancellationToken);
            using var granted = await admin.PostAsJsonAsync(new Uri($"{Realm}/users/{serviceAccount!.Id}/role-mappings/clients/{realmManagement.Id}", UriKind.Relative), new[] { viewUsers, queryGroups }, cancellationToken);
            granted.StatusCode.ShouldBe(HttpStatusCode.NoContent);

            using var spike = new HttpClient { BaseAddress = aspire.App.GetEndpoint("keycloak", "http") };
            using var form = new FormUrlEncodedContent(new Dictionary<string, string> { ["grant_type"] = "client_credentials", ["client_id"] = clientId, ["client_secret"] = "spike" });
            using var minted = await spike.PostAsync(new Uri("/realms/fabric-pilot/protocol/openid-connect/token", UriKind.Relative), form, cancellationToken);
            minted.EnsureSuccessStatusCode();
            var token = await minted.Content.ReadFromJsonAsync<Token>(cancellationToken);
            spike.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token!.AccessToken);
            var email = $"spike-{Guid.CreateVersion7():N}@example.com";

            using var response = await spike.PostAsJsonAsync(new Uri($"{Realm}/users", UriKind.Relative), new { username = email, email, enabled = true, groups = TenantAGroups }, cancellationToken);

            response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        }
        finally
        {
            using var deleted = await admin.DeleteAsync(new Uri($"{Realm}/clients/{client.Id}", UriKind.Relative), cancellationToken);
        }
    }

    private sealed record Client(string Id);

    private sealed record Role(string Id, string Name);

    private sealed record Token([property: JsonPropertyName("access_token")] string AccessToken);
}
