using System.Net;
using System.Net.Http.Json;

namespace FabricPilot.Integration.Tests;

/// <summary>
/// A customer admin manages their own tenant's users through Keycloak, the only store (#14). Each test invents its own
/// addresses, since the run shares one realm, and reads the result back from Keycloak as its administrator.
/// </summary>
public class IdentityTenantUsersTests(AspireFixture aspire)
{
    private static readonly Uri Users = new("/identity/users", UriKind.Relative);

    private static readonly string[] TenantBGroups = ["/tenants/tenant-b"];

    private readonly KeycloakRealm realm = new(aspire);
    private readonly MailpitInbox mailpit = new(aspire);

    [Fact]
    public async Task An_invited_user_is_created_in_the_admins_tenant_with_the_chosen_role_and_sent_an_invitation()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var email = NewAddress();
        using var developer = await aspire.CreateHostClientAsync("developer");

        using var response = await developer.PostAsJsonAsync(Users, new { email = email.ToUpperInvariant(), firstName = "New", lastName = "User", role = "customer-user" }, cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var user = (await realm.FindAsync(email, cancellationToken)).ShouldNotBeNull();
        (await response.Content.ReadFromJsonAsync<Guid>(cancellationToken)).ShouldBe(user.Id);
        response.Headers.Location.ShouldNotBeNull().ToString().ShouldEndWith($"/identity/users/{user.Id}");
        user.Email.ShouldBe(email);
        user.Groups.ShouldBe(["/tenants/tenant-a"]);
        user.MappedRoles.ShouldContain("customer-user");
        user.EffectiveRoles.ShouldNotContain("customer-admin");
        user.RequiredActions.ShouldBe(["VERIFY_EMAIL", "UPDATE_PASSWORD"], ignoreOrder: true);
        (await mailpit.CountForAsync(email, 1, cancellationToken)).ShouldBe(1);
    }

    [Theory]
    [InlineData("staff")]
    [InlineData("manager")]
    public async Task A_customer_admin_cannot_grant_a_role_outside_the_tenants_roles(string role)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var email = NewAddress();
        using var developer = await aspire.CreateHostClientAsync("developer");

        using var response = await developer.PostAsJsonAsync(Users, new { email, firstName = "New", lastName = "User", role }, cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await response.Content.ReadAsStringAsync(cancellationToken)).ShouldContain("identity.role-not-grantable");
        (await realm.FindAsync(email, cancellationToken)).ShouldBeNull();
    }

    [Fact]
    public async Task A_tenant_in_the_request_body_is_ignored()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var email = NewAddress();
        using var developer = await aspire.CreateHostClientAsync("developer");

        using var response = await developer.PostAsJsonAsync(Users, new { email, firstName = "New", lastName = "User", role = "customer-user", tenant = "tenant-b", tenantId = "tenant-b", groups = TenantBGroups }, cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        (await realm.FindAsync(email, cancellationToken)).ShouldNotBeNull().Groups.ShouldBe(["/tenants/tenant-a"]);
    }

    [Fact]
    public async Task A_customer_admin_cannot_change_a_user_of_another_tenant()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var developerB = await realm.FindAsync("developer-b", cancellationToken);
        using var developer = await aspire.CreateHostClientAsync("developer");

        using var response = await developer.PutAsJsonAsync(new Uri($"/identity/users/{developerB!.Id}/role", UriKind.Relative), new { role = "customer-admin" }, cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync(cancellationToken)).ShouldContain("identity.user-not-found");
        var after = await realm.FindAsync("developer-b", cancellationToken);
        after!.MappedRoles.ShouldBe(developerB.MappedRoles, ignoreOrder: true);
        after.Groups.ShouldBe(["/tenants/tenant-b"]);
    }

    [Fact]
    public async Task An_admin_of_the_other_tenant_cannot_change_a_user_of_this_one_either()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var invited = await InviteAsync("developer", "customer-user", cancellationToken);
        using var tenantBAdmin = await aspire.CreateHostClientAsync("tenant-b-admin");

        using var response = await tenantBAdmin.PutAsJsonAsync(new Uri($"/identity/users/{invited.Id}/role", UriKind.Relative), new { role = "customer-admin" }, cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await realm.FindAsync(invited.Username, cancellationToken))!.EffectiveRoles.ShouldNotContain("customer-admin");
    }

    [Fact]
    public async Task A_staff_member_is_not_found_by_a_customer_admin()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var staff = await realm.IdOfAsync("staff", cancellationToken);
        using var developer = await aspire.CreateHostClientAsync("developer");

        using var response = await developer.PutAsJsonAsync(new Uri($"/identity/users/{staff}/role", UriKind.Relative), new { role = "customer-user" }, cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Inviting_an_address_another_tenant_uses_is_a_conflict_that_leaves_its_user_alone()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var tenantBAdmin = await aspire.CreateHostClientAsync("tenant-b-admin");

        using var response = await tenantBAdmin.PostAsJsonAsync(Users, new { email = "developer@example.com", firstName = "Local", lastName = "Developer", role = "customer-user" }, cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await response.Content.ReadAsStringAsync(cancellationToken)).ShouldContain("identity.user-already-exists");
        (await realm.FindAsync("developer", cancellationToken))!.Groups.ShouldBe(["/tenants/tenant-a"]);
    }

    [Fact]
    public async Task Inviting_the_same_address_twice_is_a_conflict()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var invited = await InviteAsync("developer", "customer-user", cancellationToken);
        using var developer = await aspire.CreateHostClientAsync("developer");

        using var response = await developer.PostAsJsonAsync(Users, new { email = invited.Username, firstName = "New", lastName = "User", role = "customer-user" }, cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await response.Content.ReadAsStringAsync(cancellationToken)).ShouldContain("identity.user-already-exists");
    }

    [Fact]
    public async Task A_customer_admin_cannot_change_their_own_role()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var self = await realm.FindAsync("developer", cancellationToken);
        using var developer = await aspire.CreateHostClientAsync("developer");

        using var response = await developer.PutAsJsonAsync(new Uri($"/identity/users/{self!.Id}/role", UriKind.Relative), new { role = "customer-user" }, cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await response.Content.ReadAsStringAsync(cancellationToken)).ShouldContain("identity.cannot-change-own-role");
        (await realm.FindAsync("developer", cancellationToken))!.MappedRoles.ShouldContain("customer-admin");
    }

    [Fact]
    public async Task A_customer_admin_promotes_a_user_of_their_tenant_and_the_previous_role_is_replaced()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var invited = await InviteAsync("developer", "customer-user", cancellationToken);
        using var developer = await aspire.CreateHostClientAsync("developer");

        using var response = await developer.PutAsJsonAsync(new Uri($"/identity/users/{invited.Id}/role", UriKind.Relative), new { role = "customer-admin" }, cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var user = await realm.FindAsync(invited.Username, cancellationToken);
        user!.MappedRoles.ShouldContain("customer-admin");
        user.MappedRoles.ShouldNotContain("customer-user");
    }

    [Fact]
    public async Task A_customer_admin_cannot_grant_staff_to_a_user_of_their_tenant()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var invited = await InviteAsync("developer", "customer-user", cancellationToken);
        using var developer = await aspire.CreateHostClientAsync("developer");

        using var response = await developer.PutAsJsonAsync(new Uri($"/identity/users/{invited.Id}/role", UriKind.Relative), new { role = "staff" }, cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await realm.FindAsync(invited.Username, cancellationToken))!.EffectiveRoles.ShouldNotContain("staff");
    }

    [Fact]
    public async Task The_list_holds_only_the_callers_tenant_with_each_users_tenant_roles()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var developer = await aspire.CreateHostClientAsync("developer");

        var list = await developer.GetFromJsonAsync<Paged>(new Uri("/identity/users?pageSize=100", UriKind.Relative), cancellationToken);

        var emails = list!.Items.Select(user => user.Email).ToList();
        emails.ShouldContain("developer@example.com");
        emails.ShouldContain("visitor@example.com");
        emails.ShouldNotContain("developer-b@example.com");
        emails.ShouldNotContain("tenant-b-admin@example.com");
        emails.ShouldNotContain("staff@example.com");
        list.Items.Single(user => user.Email == "developer@example.com").Roles.ShouldBe(["customer-user", "customer-admin"]);
        list.TotalCount.ShouldBeGreaterThanOrEqualTo(list.Items.Count);
    }

    [Fact]
    public async Task An_invitation_is_sent_again_on_request()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var invited = await InviteAsync("developer", "customer-user", cancellationToken);
        (await mailpit.CountForAsync(invited.Username, 1, cancellationToken)).ShouldBe(1);
        using var developer = await aspire.CreateHostClientAsync("developer");

        using var response = await developer.PutAsync(new Uri($"/identity/users/{invited.Id}/invitation", UriKind.Relative), content: null, cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await mailpit.CountForAsync(invited.Username, 2, cancellationToken)).ShouldBe(2);
    }

    private static string NewAddress() => $"invited-{Guid.CreateVersion7():N}@example.com";

    private async Task<KeycloakRealm.RealmUser> InviteAsync(string admin, string role, CancellationToken cancellationToken)
    {
        var email = NewAddress();
        using var client = await aspire.CreateHostClientAsync(admin);
        using var response = await client.PostAsJsonAsync(Users, new { email, firstName = "New", lastName = "User", role }, cancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await realm.FindAsync(email, cancellationToken)).ShouldNotBeNull();
    }

    private sealed record Paged(IReadOnlyList<TenantUser> Items, int TotalCount);

    private sealed record TenantUser(Guid Id, string Email, IReadOnlyList<string> Roles);
}
