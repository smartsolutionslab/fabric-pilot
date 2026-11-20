using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace FabricPilot.Integration.Tests;

/// <summary>
/// Staff list the realm's staff, the users holding the realm role staff, to pick an assignee from:
/// identity.list-staff requires staff, which staff and staff-b hold and no customer does.
/// </summary>
public class IdentityStaffTests(AspireFixture aspire)
{
    private const string Staff = "/identity/staff?page=1&pageSize=100";

    [Fact]
    public async Task Staff_list_every_staff_member_by_user_name_and_no_customer()
    {
        using var staff = await aspire.CreateHostClientAsync("staff");

        using var response = await staff.GetAsync(new Uri(Staff, UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var page = (await response.Content.ReadFromJsonAsync<JsonObject>(TestContext.Current.CancellationToken))!;
        var members = page["items"]!.AsArray().Select(item => item!.AsObject()).ToList();
        members.Select(member => member["userName"]!.GetValue<string>()).ShouldBe(["staff", "staff-b"]);
        members[0]["firstName"]!.GetValue<string>().ShouldBe("Local");
        members[0]["lastName"]!.GetValue<string>().ShouldBe("Staff");
        page["totalCount"]!.GetValue<int>().ShouldBe(2);
    }

    [Fact]
    public async Task Staff_page_through_the_staff()
    {
        using var staff = await aspire.CreateHostClientAsync("staff");

        using var response = await staff.GetAsync(new Uri("/identity/staff?page=2&pageSize=1", UriKind.Relative), TestContext.Current.CancellationToken);

        var page = (await response.Content.ReadFromJsonAsync<JsonObject>(TestContext.Current.CancellationToken))!;
        page["items"]!.AsArray().Select(item => item!["userName"]!.GetValue<string>()).ShouldBe(["staff-b"]);
        page["totalCount"]!.GetValue<int>().ShouldBe(2);
    }

    /// <summary>developer holds customer-admin and manager, developer-b customer-user, visitor no role.</summary>
    [Theory]
    [InlineData("developer")]
    [InlineData("developer-b")]
    [InlineData("visitor")]
    public async Task A_customer_is_forbidden_to_list_the_staff(string user)
    {
        using var client = await aspire.CreateHostClientAsync(user);

        using var response = await client.GetAsync(new Uri(Staff, UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task An_anonymous_caller_is_unauthorized_to_list_the_staff()
    {
        using var client = aspire.CreateHostClient();

        using var response = await client.GetAsync(new Uri(Staff, UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
