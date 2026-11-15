using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace FabricPilot.Integration.Tests;

/// <summary>
/// Staff record a contract for a customer tenant: the tenant's own users then list it, another tenant's do not. No
/// tenant's user records one, whatever their roles, and a tenant the host does not know is not found.
/// </summary>
public class ContractsRecordTests(AspireFixture aspire)
{
    private const string Contracts = "/contracts/contracts?page=1&pageSize=100";

    [Fact]
    public async Task A_contract_staff_record_for_a_tenant_is_listed_for_that_tenants_users_with_its_renewal_date_and_not_for_another_tenants()
    {
        var product = $"Managed printing {Guid.CreateVersion7()}";
        using var staff = await aspire.CreateHostClientAsync("staff");

        using var response = await staff.PostAsJsonAsync(
            new Uri(StaffRecordedContracts.For("tenant-a"), UriKind.Relative),
            new { product, startsOn = "2026-01-01", endsOn = "2026-12-31", renews = true },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var id = await response.Content.ReadFromJsonAsync<Guid>(TestContext.Current.CancellationToken);
        using var developer = await aspire.CreateHostClientAsync("developer");
        var listed = (await ListAsync(developer)).Single(item => item!["id"]!.GetValue<Guid>() == id)!;
        listed["product"]!.GetValue<string>().ShouldBe(product);
        listed["startsOn"]!.GetValue<string>().ShouldBe("2026-01-01");
        listed["endsOn"]!.GetValue<string>().ShouldBe("2026-12-31");
        listed["renewsOn"]!.GetValue<string>().ShouldBe("2027-01-01");
        using var otherTenant = await aspire.CreateHostClientAsync("developer-b");
        (await ListAsync(otherTenant)).ShouldNotContain(item => item!["id"]!.GetValue<Guid>() == id);
    }

    [Fact]
    public async Task A_contract_that_breaks_a_rule_is_refused_with_the_domains_code_and_nothing_is_listed()
    {
        var product = $"Hosting {Guid.CreateVersion7()}";
        using var staff = await aspire.CreateHostClientAsync("staff");

        using var response = await staff.PostAsJsonAsync(
            new Uri(StaffRecordedContracts.For("tenant-a"), UriKind.Relative),
            new { product, startsOn = "2026-12-31", endsOn = "2026-01-01", renews = true },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await ProblemCodeAsync(response)).ShouldBe("contracts.ends-before-it-starts");
        using var developer = await aspire.CreateHostClientAsync("developer");
        (await ListAsync(developer)).ShouldNotContain(item => item!["product"]!.GetValue<string>() == product);
    }

    [Fact]
    public async Task A_tenant_the_host_does_not_know_is_not_found()
    {
        using var staff = await aspire.CreateHostClientAsync("staff");

        using var response = await staff.PostAsJsonAsync(
            new Uri(StaffRecordedContracts.For("tenant-aa"), UriKind.Relative),
            new { product = "Hosting", startsOn = "2026-01-01", endsOn = "2026-12-31", renews = true },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await ProblemCodeAsync(response)).ShouldBe("tenant-not-found");
    }

    [Theory]
    [InlineData("visitor")]
    [InlineData("developer")]
    [InlineData("developer-b")]
    public async Task A_tenants_user_is_forbidden_whatever_their_roles_even_for_their_own_tenant(string user)
    {
        var product = $"Hosting {Guid.CreateVersion7()}";
        using var client = await aspire.CreateHostClientAsync(user);

        using var response = await client.PostAsJsonAsync(
            new Uri(StaffRecordedContracts.For(user == "developer-b" ? "tenant-b" : "tenant-a"), UriKind.Relative),
            new { product, startsOn = "2026-01-01", endsOn = "2026-12-31", renews = true },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await ListAsync(client)).ShouldNotContain(item => item!["product"]!.GetValue<string>() == product);
    }

    [Fact]
    public async Task An_anonymous_caller_is_unauthorized()
    {
        using var client = aspire.CreateHostClient();

        using var response = await client.PostAsJsonAsync(
            new Uri(StaffRecordedContracts.For("tenant-a"), UriKind.Relative),
            new { product = "Hosting", startsOn = "2026-01-01", endsOn = "2026-12-31", renews = true },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private static async Task<JsonArray> ListAsync(HttpClient client)
    {
        using var response = await client.GetAsync(new Uri(Contracts, UriKind.Relative), TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        return JsonNode.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))!["items"]!.AsArray();
    }

    private static async Task<string?> ProblemCodeAsync(HttpResponseMessage response) =>
        JsonNode.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))!["code"]?.GetValue<string>();
}
