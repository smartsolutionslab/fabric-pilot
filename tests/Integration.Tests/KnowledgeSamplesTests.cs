using System.Net;
using System.Net.Http.Json;

namespace FabricPilot.Integration.Tests;

/// <summary>
/// The knowledge module's sample through the running application: a sample is created, read back and listed a
/// page at a time, inside the tenant of the user who created it. It goes with the sample (AGENTS.md).
/// </summary>
public class KnowledgeSamplesTests(AspireFixture aspire)
{
    private static readonly Uri Samples = new("/knowledge/samples", UriKind.Relative);

    [Fact]
    public async Task A_created_sample_is_read_back_and_listed()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = await aspire.CreateHostClientAsync("developer");
        var name = $"Sample {Guid.CreateVersion7()}";

        using var created = await client.PostAsJsonAsync(Samples, new { name }, cancellationToken);

        created.StatusCode.ShouldBe(HttpStatusCode.Created);
        var id = await created.Content.ReadFromJsonAsync<Guid>(cancellationToken);
        created.Headers.Location.ShouldBe(new Uri($"/knowledge/samples/{id}", UriKind.Relative));
        var sample = await client.GetFromJsonAsync<Sample>(created.Headers.Location, cancellationToken);
        sample.ShouldBe(new Sample(id, name));
        var page = await client.GetFromJsonAsync<SamplePage>(new Uri("/knowledge/samples?page=1&pageSize=100", UriKind.Relative), cancellationToken);
        page!.Items.ShouldContain(new Sample(id, name));
        page.TotalCount.ShouldBeGreaterThan(0);
        page.PageSize.ShouldBe(100);
    }

    [Fact]
    public async Task A_blank_name_is_refused_as_the_callers_mistake()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = await aspire.CreateHostClientAsync("developer");

        using var refused = await client.PostAsJsonAsync(Samples, new { name = "  " }, cancellationToken);

        refused.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await refused.Content.ReadFromJsonAsync<Problem>(cancellationToken))!.Code.ShouldBe("knowledge.invalid-sample-name");
    }

    [Fact]
    public async Task A_page_size_above_the_maximum_is_refused()
    {
        using var client = await aspire.CreateHostClientAsync("developer");

        using var response = await client.GetAsync(new Uri("/knowledge/samples?pageSize=101", UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task A_user_without_the_manage_permission_lists_samples_but_creates_none()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = await aspire.CreateHostClientAsync("visitor");

        using var listed = await client.GetAsync(Samples, cancellationToken);
        using var created = await client.PostAsJsonAsync(Samples, new { name = "Not mine to create" }, cancellationToken);

        listed.StatusCode.ShouldBe(HttpStatusCode.OK);
        created.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task An_anonymous_caller_neither_lists_nor_creates_samples()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = aspire.CreateHostClient();

        using var listed = await client.GetAsync(Samples, cancellationToken);
        using var created = await client.PostAsJsonAsync(Samples, new { name = "Anonymous" }, cancellationToken);

        listed.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        created.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_user_of_another_tenant_neither_reads_nor_lists_the_sample()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var developer = await aspire.CreateHostClientAsync("developer");
        using var otherTenant = await aspire.CreateHostClientAsync("developer-b");
        var name = $"Tenant A {Guid.CreateVersion7()}";
        using var created = await developer.PostAsJsonAsync(Samples, new { name }, cancellationToken);
        var id = await created.Content.ReadFromJsonAsync<Guid>(cancellationToken);

        using var read = await otherTenant.GetAsync(created.Headers.Location, cancellationToken);
        var page = await otherTenant.GetFromJsonAsync<SamplePage>(new Uri("/knowledge/samples?pageSize=100", UriKind.Relative), cancellationToken);

        read.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        page!.Items.ShouldNotContain(new Sample(id, name));
    }

    private sealed record Sample(Guid Id, string Name);

    private sealed record SamplePage(IReadOnlyList<Sample> Items, int TotalCount, int Page, int PageSize);

    private sealed record Problem(string Code);
}
