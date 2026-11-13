using System.Net;
using System.Net.Http.Json;

namespace FabricPilot.Integration.Tests;

/// <summary>
/// Staff, who belong to no tenant, manage any tenant's admins on endpoints that name the tenant in the route, under a
/// staff-only policy and not AcrossTenants() (the owner's decision on #14, docs/adr).
/// </summary>
public class IdentityTenantAdminsTests(AspireFixture aspire)
{
    private readonly KeycloakRealm realm = new(aspire);

    [Fact]
    public async Task Staff_invite_an_admin_into_the_tenant_the_route_names()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var email = NewAddress();
        using var staff = await aspire.CreateHostClientAsync("staff");

        using var response = await staff.PostAsJsonAsync(new Uri("/identity/tenants/tenant-b/admins", UriKind.Relative), new { email, firstName = "New", lastName = "Admin" }, cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var user = (await realm.FindAsync(email, cancellationToken)).ShouldNotBeNull();
        user.Groups.ShouldBe(["/tenants/tenant-b"]);
        user.MappedRoles.ShouldContain("customer-admin");
        user.EffectiveRoles.ShouldNotContain("staff");
    }

    [Fact]
    public async Task Staff_inviting_into_a_tenant_the_realm_does_not_have_get_not_found()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var email = NewAddress();
        using var staff = await aspire.CreateHostClientAsync("staff");

        using var response = await staff.PostAsJsonAsync(new Uri("/identity/tenants/tenant-c/admins", UriKind.Relative), new { email, firstName = "New", lastName = "Admin" }, cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync(cancellationToken)).ShouldContain("identity.tenant-not-found");
        (await realm.FindAsync(email, cancellationToken)).ShouldBeNull();
    }

    [Fact]
    public async Task Staff_demote_a_tenants_admin_to_a_user()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var admin = await InviteAdminAsync("tenant-b", cancellationToken);
        using var staff = await aspire.CreateHostClientAsync("staff");

        using var response = await staff.PutAsJsonAsync(new Uri($"/identity/tenants/tenant-b/admins/{admin.Id}/role", UriKind.Relative), new { role = "customer-user" }, cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var user = await realm.FindAsync(admin.Username, cancellationToken);
        user!.MappedRoles.ShouldContain("customer-user");
        user.EffectiveRoles.ShouldNotContain("customer-admin");
    }

    [Fact]
    public async Task Staff_cannot_grant_staff_to_a_tenants_user()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var admin = await InviteAdminAsync("tenant-b", cancellationToken);
        using var staff = await aspire.CreateHostClientAsync("staff");

        using var response = await staff.PutAsJsonAsync(new Uri($"/identity/tenants/tenant-b/admins/{admin.Id}/role", UriKind.Relative), new { role = "staff" }, cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await response.Content.ReadAsStringAsync(cancellationToken)).ShouldContain("identity.role-not-grantable");
        (await realm.FindAsync(admin.Username, cancellationToken))!.EffectiveRoles.ShouldNotContain("staff");
    }

    [Fact]
    public async Task A_user_of_another_tenant_than_the_route_names_is_not_found()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var developer = await realm.IdOfAsync("developer", cancellationToken);
        using var staff = await aspire.CreateHostClientAsync("staff");

        using var response = await staff.PutAsJsonAsync(new Uri($"/identity/tenants/tenant-b/admins/{developer}/role", UriKind.Relative), new { role = "customer-user" }, cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await realm.FindAsync("developer", cancellationToken))!.MappedRoles.ShouldContain("customer-admin");
    }

    [Fact]
    public async Task Staff_list_a_tenants_admins_and_nobody_else()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var staff = await aspire.CreateHostClientAsync("staff");

        var list = await staff.GetFromJsonAsync<Paged>(new Uri("/identity/tenants/tenant-b/admins?pageSize=100", UriKind.Relative), cancellationToken);

        var emails = list!.Items.Select(user => user.Email).ToList();
        emails.ShouldContain("tenant-b-admin@example.com");
        emails.ShouldNotContain("developer-b@example.com");
        emails.ShouldNotContain("developer@example.com");
    }

    private static string NewAddress() => $"admin-{Guid.CreateVersion7():N}@example.com";

    private async Task<KeycloakRealm.RealmUser> InviteAdminAsync(string tenant, CancellationToken cancellationToken)
    {
        var email = NewAddress();
        using var staff = await aspire.CreateHostClientAsync("staff");
        using var response = await staff.PostAsJsonAsync(new Uri($"/identity/tenants/{tenant}/admins", UriKind.Relative), new { email, firstName = "New", lastName = "Admin" }, cancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await realm.FindAsync(email, cancellationToken)).ShouldNotBeNull();
    }

    private sealed record Paged(IReadOnlyList<TenantUser> Items, int TotalCount);

    private sealed record TenantUser(Guid Id, string Email);
}
