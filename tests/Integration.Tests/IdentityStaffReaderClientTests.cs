using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace FabricPilot.Integration.Tests;

/// <summary>
/// The identity module's second confidential client, fabric-pilot-staff-reader, lists the staff role's members and holds
/// the least that lets it. A spike against Keycloak 26.6 found that the role's members list refuses view-users and
/// query-users, alone or together: Keycloak also checks that the caller may view the role, which for a realm role is
/// view-realm. So the client holds query-users and view-realm, both read-only, and the write client keeps manage-users
/// alone (docs/adr/0053).
/// </summary>
public class IdentityStaffReaderClientTests(AspireFixture aspire)
{
    private const string Realm = "/admin/realms/fabric-pilot";

    private const string StaffMembers = $"{Realm}/roles/staff/users?first=0&max=10&briefRepresentation=true";

    private readonly KeycloakRealm realm = new(aspire);

    [Fact]
    public async Task The_staff_reader_service_account_holds_query_users_and_view_realm_and_no_other_realm_management_role()
    {
        var roles = await realm.ServiceAccountAdminRolesAsync("fabric-pilot-staff-reader", TestContext.Current.CancellationToken);

        roles.ShouldBe(["query-users", "view-realm"]);
    }

    [Fact]
    public async Task The_user_admin_service_account_still_holds_manage_users_alone()
    {
        var roles = await realm.ServiceAccountAdminRolesAsync(TestContext.Current.CancellationToken);

        roles.ShouldBe(["manage-users"]);
    }

    [Fact]
    public async Task The_staff_reader_lists_the_staff_roles_members()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var reader = await ClientAsync("fabric-pilot-staff-reader", await StaffReaderSecretAsync(cancellationToken), cancellationToken);

        using var response = await reader.GetAsync(new Uri(StaffMembers, UriKind.Relative), cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task The_staff_reader_cannot_create_a_user()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var reader = await ClientAsync("fabric-pilot-staff-reader", await StaffReaderSecretAsync(cancellationToken), cancellationToken);
        var email = $"reader-{Guid.CreateVersion7():N}@example.com";

        using var response = await reader.PostAsJsonAsync(new Uri($"{Realm}/users", UriKind.Relative), new { username = email, email, enabled = true }, cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    /// <summary>Neither of the two roles is enough alone, nor is view-users, which the design proposed.</summary>
    [Theory]
    [InlineData("query-users")]
    [InlineData("view-realm")]
    [InlineData("view-users")]
    public async Task With_one_role_alone_the_admin_api_refuses_to_list_the_staff_roles_members(string role)
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
            var granted = await admin.GetFromJsonAsync<Role>(new Uri($"{Realm}/clients/{realmManagement.Id}/roles/{role}", UriKind.Relative), cancellationToken);
            using var mapped = await admin.PostAsJsonAsync(new Uri($"{Realm}/users/{serviceAccount!.Id}/role-mappings/clients/{realmManagement.Id}", UriKind.Relative), new[] { granted }, cancellationToken);
            mapped.StatusCode.ShouldBe(HttpStatusCode.NoContent);
            using var spike = await ClientAsync(clientId, "spike", cancellationToken);

            using var response = await spike.GetAsync(new Uri(StaffMembers, UriKind.Relative), cancellationToken);

            response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        }
        finally
        {
            using var deleted = await admin.DeleteAsync(new Uri($"{Realm}/clients/{client.Id}", UriKind.Relative), cancellationToken);
        }
    }

    private async Task<string> StaffReaderSecretAsync(CancellationToken cancellationToken)
    {
        var secret = aspire.App.Services.GetRequiredService<DistributedApplicationModel>().Resources.OfType<ParameterResource>().Single(parameter => parameter.Name == "keycloak-staff-reader-secret");
        return await secret.GetValueAsync(cancellationToken) ?? string.Empty;
    }

    /// <summary>An HttpClient calling Keycloak with the client's service-account token.</summary>
    private async Task<HttpClient> ClientAsync(string clientId, string secret, CancellationToken cancellationToken)
    {
        var client = new HttpClient { BaseAddress = aspire.App.GetEndpoint("keycloak", "http") };
        using var form = new FormUrlEncodedContent(new Dictionary<string, string> { ["grant_type"] = "client_credentials", ["client_id"] = clientId, ["client_secret"] = secret });
        using var minted = await client.PostAsync(new Uri("/realms/fabric-pilot/protocol/openid-connect/token", UriKind.Relative), form, cancellationToken);
        minted.EnsureSuccessStatusCode();
        var token = await minted.Content.ReadFromJsonAsync<Token>(cancellationToken);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.AccessToken);
        return client;
    }

    private sealed record Client(string Id);

    private sealed record Role(string Id, string Name);

    private sealed record Token([property: JsonPropertyName("access_token")] string AccessToken);
}
