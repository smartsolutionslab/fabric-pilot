using System.Net;
using System.Text.Json.Nodes;
using FabricPilot.Modules.Contracts.Domain.Contracts;

namespace FabricPilot.Integration.Tests;

/// <summary>
/// Staff list the contracts every tenant has up for renewal, the soonest first; a tenant's user, whatever their roles,
/// is refused. The host's clock is the wall clock, so the contracts renew relative to today.
/// </summary>
public class ContractsRenewalListTests(AspireFixture aspire)
{
    private const string UpForRenewal = "/contracts/contracts/up-for-renewal?page=1&pageSize=100";

    [Fact]
    public async Task Staff_see_every_tenants_contracts_up_for_renewal_the_soonest_first_and_no_others()
    {
        var today = DateOnly.FromDateTime(TimeProvider.System.GetUtcNow().UtcDateTime);
        var tenantA = await RenewingAsync("tenant-a", $"Managed printing {Guid.CreateVersion7()}", today.AddDays(30));
        var tenantB = await RenewingAsync("tenant-b", $"Hosting {Guid.CreateVersion7()}", today.AddDays(60));
        var tooLate = await RenewingAsync("tenant-a", $"Backup {Guid.CreateVersion7()}", today.AddDays(Contract.RenewalWindowDays + 30));
        var ending = await StaffRecordedContracts.RecordAsync(aspire, "tenant-a", $"Migration project {Guid.CreateVersion7()}", today.AddDays(-100), today.AddDays(10), renews: false, TestContext.Current.CancellationToken);

        using var client = await aspire.CreateHostClientAsync("staff");
        using var response = await client.GetAsync(new Uri(UpForRenewal, UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var ids = JsonNode.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))!["items"]!.AsArray()
            .Select(item => item!["id"]!.GetValue<Guid>())
            .ToList();
        ids.ShouldContain(tenantA);
        ids.ShouldContain(tenantB);
        ids.IndexOf(tenantA).ShouldBeLessThan(ids.IndexOf(tenantB));
        ids.ShouldNotContain(tooLate);
        ids.ShouldNotContain(ending);
    }

    [Theory]
    [InlineData("visitor")]
    [InlineData("developer")]
    [InlineData("developer-b")]
    public async Task A_tenants_user_is_forbidden_whatever_their_roles(string user)
    {
        using var client = await aspire.CreateHostClientAsync(user);

        using var response = await client.GetAsync(new Uri(UpForRenewal, UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task An_anonymous_caller_is_unauthorized()
    {
        using var client = aspire.CreateHostClient();

        using var response = await client.GetAsync(new Uri(UpForRenewal, UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    /// <summary>A contract whose last day is the day before <paramref name="renewsOn"/>, renewing then.</summary>
    private Task<Guid> RenewingAsync(string tenant, string product, DateOnly renewsOn) =>
        StaffRecordedContracts.RecordAsync(aspire, tenant, product, renewsOn.AddYears(-1), renewsOn.AddDays(-1), renews: true, TestContext.Current.CancellationToken);
}
