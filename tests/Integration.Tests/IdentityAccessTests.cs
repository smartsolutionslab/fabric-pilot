using System.Net;
using System.Net.Http.Json;

namespace FabricPilot.Integration.Tests;

/// <summary>
/// Who may call each identity endpoint: identity.manage-users requires customer-admin, which developer holds (tenant-a)
/// and developer-b (customer-user), visitor (no role) and staff do not; identity.manage-tenant-admins requires staff,
/// which no customer holds.
/// </summary>
public class IdentityAccessTests(AspireFixture aspire)
{
    private static readonly object Invitation = new { email = "nobody@example.com", firstName = "No", lastName = "Body", role = "customer-user" };

    private static readonly object Role = new { role = "customer-user" };

    private readonly KeycloakRealm realm = new(aspire);

    public static TheoryData<string, string> CustomerEndpoints => new()
    {
        { "GET", "/identity/users" },
        { "POST", "/identity/users" },
        { "PUT", $"/identity/users/{Guid.CreateVersion7()}/role" },
        { "PUT", $"/identity/users/{Guid.CreateVersion7()}/invitation" },
    };

    public static TheoryData<string, string> StaffEndpoints => new()
    {
        { "GET", "/identity/tenants/tenant-b/admins" },
        { "POST", "/identity/tenants/tenant-b/admins" },
        { "PUT", $"/identity/tenants/tenant-b/admins/{Guid.CreateVersion7()}/role" },
    };

    [Fact]
    public async Task A_customer_admin_lists_their_tenants_users()
    {
        using var developer = await aspire.CreateHostClientAsync("developer");

        using var response = await developer.GetAsync(new Uri("/identity/users", UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task A_customer_admin_invites_a_user()
    {
        using var developer = await aspire.CreateHostClientAsync("developer");

        using var response = await developer.PostAsJsonAsync(new Uri("/identity/users", UriKind.Relative), new { email = NewAddress(), firstName = "New", lastName = "User", role = "customer-user" }, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task A_customer_admin_changes_a_users_role()
    {
        var invited = await InviteAsync(TestContext.Current.CancellationToken);
        using var developer = await aspire.CreateHostClientAsync("developer");

        using var response = await developer.PutAsJsonAsync(new Uri($"/identity/users/{invited}/role", UriKind.Relative), new { role = "customer-admin" }, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task A_customer_admin_sends_an_invitation_again()
    {
        var invited = await InviteAsync(TestContext.Current.CancellationToken);
        using var developer = await aspire.CreateHostClientAsync("developer");

        using var response = await developer.PutAsync(new Uri($"/identity/users/{invited}/invitation", UriKind.Relative), content: null, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Theory]
    [MemberData(nameof(CustomerEndpoints))]
    public async Task A_signed_in_user_who_is_not_a_customer_admin_is_forbidden(string method, string path)
    {
        foreach (var user in new[] { "developer-b", "visitor", "staff" })
        {
            using var client = await aspire.CreateHostClientAsync(user);

            using var response = await SendAsync(client, method, path, Invitation);

            response.StatusCode.ShouldBe(HttpStatusCode.Forbidden, $"{user} {method} {path}");
        }
    }

    [Theory]
    [MemberData(nameof(CustomerEndpoints))]
    public async Task An_anonymous_caller_of_a_customer_endpoint_is_unauthorized(string method, string path)
    {
        using var client = aspire.CreateHostClient();

        using var response = await SendAsync(client, method, path, Invitation);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Staff_list_a_tenants_admins()
    {
        using var staff = await aspire.CreateHostClientAsync("staff");

        using var response = await staff.GetAsync(new Uri("/identity/tenants/tenant-b/admins", UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Staff_invite_a_tenants_admin()
    {
        using var staff = await aspire.CreateHostClientAsync("staff");

        using var response = await staff.PostAsJsonAsync(new Uri("/identity/tenants/tenant-b/admins", UriKind.Relative), new { email = NewAddress(), firstName = "New", lastName = "Admin" }, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Staff_change_a_tenants_admins_role()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var tenantBAdmin = await realm.IdOfAsync("tenant-b-admin", cancellationToken);
        using var staff = await aspire.CreateHostClientAsync("staff");

        // Granting the role tenant-b-admin holds already: allowed, and it leaves the realm as it was for the other tests.
        using var response = await staff.PutAsJsonAsync(new Uri($"/identity/tenants/tenant-b/admins/{tenantBAdmin}/role", UriKind.Relative), new { role = "customer-admin" }, cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await realm.FindAsync("tenant-b-admin", cancellationToken))!.MappedRoles.ShouldContain("customer-admin");
    }

    [Theory]
    [MemberData(nameof(StaffEndpoints))]
    public async Task A_signed_in_user_who_is_not_staff_is_forbidden(string method, string path)
    {
        foreach (var user in new[] { "developer", "tenant-b-admin", "visitor" })
        {
            using var client = await aspire.CreateHostClientAsync(user);

            using var response = await SendAsync(client, method, path, Role);

            response.StatusCode.ShouldBe(HttpStatusCode.Forbidden, $"{user} {method} {path}");
        }
    }

    [Theory]
    [MemberData(nameof(StaffEndpoints))]
    public async Task An_anonymous_caller_of_a_staff_endpoint_is_unauthorized(string method, string path)
    {
        using var client = aspire.CreateHostClient();

        using var response = await SendAsync(client, method, path, Role);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private static string NewAddress() => $"access-{Guid.CreateVersion7():N}@example.com";

    private static Task<HttpResponseMessage> SendAsync(HttpClient client, string method, string path, object body)
    {
        var uri = new Uri(path, UriKind.Relative);
        return method switch
        {
            "GET" => client.GetAsync(uri, TestContext.Current.CancellationToken),
            "POST" => client.PostAsJsonAsync(uri, body, TestContext.Current.CancellationToken),
            "PUT" => client.PutAsJsonAsync(uri, body, TestContext.Current.CancellationToken),
            _ => throw new InvalidOperationException($"{method} is not GET, POST or PUT."),
        };
    }

    private async Task<Guid> InviteAsync(CancellationToken cancellationToken)
    {
        var email = NewAddress();
        using var developer = await aspire.CreateHostClientAsync("developer");
        using var response = await developer.PostAsJsonAsync(new Uri("/identity/users", UriKind.Relative), new { email, firstName = "New", lastName = "User", role = "customer-user" }, cancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return await realm.IdOfAsync(email, cancellationToken);
    }
}
