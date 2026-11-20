using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace FabricPilot.Integration.Tests;

/// <summary>Contracts staff record for a tenant through the module's own endpoint, as the tests need them.</summary>
public static class StaffRecordedContracts
{
    /// <summary>The endpoint staff record a contract for <c>{tenant}</c> with.</summary>
    public static string For(string tenant) => $"/contracts/contracts/{tenant}";

    /// <summary>Records the contract for the tenant as <c>staff</c> and answers its id.</summary>
    public static async Task<Guid> RecordAsync(AspireFixture aspire, string tenant, string product, DateOnly startsOn, DateOnly endsOn, bool renews, CancellationToken cancellationToken)
    {
        Ensure.That(aspire).IsNotNull();

        using var staff = await aspire.CreateHostClientAsync("staff");
        using var response = await staff.PostAsJsonAsync(new Uri(For(tenant), UriKind.Relative), new { product, startsOn, endsOn, renews }, cancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync(cancellationToken));
        return JsonSerializer.Deserialize<Guid>(await response.Content.ReadAsStringAsync(cancellationToken));
    }
}
