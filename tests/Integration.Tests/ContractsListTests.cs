using System.Net;
using System.Text.Json.Nodes;

namespace FabricPilot.Integration.Tests;

/// <summary>A customer lists their own tenant's contracts, with the renewal date the domain gives each, and never another tenant's.</summary>
public class ContractsListTests(AspireFixture aspire)
{
    private const string Contracts = "/contracts/contracts?page=1&pageSize=100";

    [Fact]
    public async Task A_customer_sees_their_tenants_contracts_with_their_renewal_dates_and_not_another_tenants()
    {
        var renewingProduct = $"Managed printing {Guid.CreateVersion7()}";
        var renewing = await StaffRecordedContracts.RecordAsync(aspire, "tenant-a", renewingProduct, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), renews: true, TestContext.Current.CancellationToken);
        var ending = await StaffRecordedContracts.RecordAsync(aspire, "tenant-a", $"Migration project {Guid.CreateVersion7()}", new DateOnly(2026, 3, 1), new DateOnly(2026, 8, 31), renews: false, TestContext.Current.CancellationToken);
        var otherTenants = await StaffRecordedContracts.RecordAsync(aspire, "tenant-b", $"Hosting {Guid.CreateVersion7()}", new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), renews: true, TestContext.Current.CancellationToken);

        using var client = await aspire.CreateHostClientAsync("visitor");
        var items = (await Json(client, Contracts))["items"]!.AsArray();

        var listedRenewing = items.Single(item => item!["id"]!.GetValue<Guid>() == renewing)!;
        listedRenewing["product"]!.GetValue<string>().ShouldBe(renewingProduct);
        listedRenewing["startsOn"]!.GetValue<string>().ShouldBe("2026-01-01");
        listedRenewing["endsOn"]!.GetValue<string>().ShouldBe("2026-12-31");
        listedRenewing["renewsOn"]!.GetValue<string>().ShouldBe("2027-01-01");
        var listedEnding = items.Single(item => item!["id"]!.GetValue<Guid>() == ending)!;
        listedEnding["endsOn"]!.GetValue<string>().ShouldBe("2026-08-31");
        listedEnding["renewsOn"].ShouldBeNull();
        items.ShouldNotContain(item => item!["id"]!.GetValue<Guid>() == otherTenants);

        using var otherClient = await aspire.CreateHostClientAsync("developer-b");
        var otherItems = (await Json(otherClient, Contracts))["items"]!.AsArray();
        otherItems.ShouldContain(item => item!["id"]!.GetValue<Guid>() == otherTenants);
        otherItems.ShouldNotContain(item => item!["id"]!.GetValue<Guid>() == renewing);
    }

    [Fact]
    public async Task An_anonymous_caller_is_unauthorized()
    {
        using var client = aspire.CreateHostClient();

        using var response = await client.GetAsync(new Uri(Contracts, UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private static async Task<JsonNode> Json(HttpClient client, string path)
    {
        using var response = await client.GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        return JsonNode.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))!;
    }
}
