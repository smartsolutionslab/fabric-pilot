using System.Net;
using System.Net.Http.Json;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;

namespace FabricPilot.Integration.Tests;

public class HostTests(AspireFixture aspire)
{
    private static readonly TimeSpan ResourceTimeout = TimeSpan.FromMinutes(2);

    [Fact]
    public async Task Every_health_checked_resource_becomes_healthy()
    {
        // Aspire adds helper resources of its own (a project rebuilder, for one) that never report health.
        var resources = aspire.App.Services.GetRequiredService<DistributedApplicationModel>().Resources
            .Where(resource => resource.Annotations.OfType<HealthCheckAnnotation>().Any())
            .Select(resource => resource.Name)
            .ToList();

        List<string> unhealthy = [];
        foreach (var resource in resources)
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
            timeout.CancelAfter(ResourceTimeout);
            try
            {
                await aspire.App.ResourceNotifications.WaitForResourceHealthyAsync(resource, timeout.Token);
            }
            catch (OperationCanceledException) when (!TestContext.Current.CancellationToken.IsCancellationRequested)
            {
                unhealthy.Add(resource);
            }
        }

        string.Join(", ", unhealthy).ShouldBe(string.Empty);
    }

    [Fact]
    public async Task The_host_reports_itself_healthy()
    {
        using var client = aspire.CreateHostClient();

        using var response = await client.GetAsync(new Uri("/health", UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task The_api_is_described_by_an_openapi_document()
    {
        using var client = aspire.CreateHostClient();

        using var response = await client.GetAsync(new Uri("/openapi/v1.json", UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task An_anonymous_caller_is_rejected()
    {
        using var client = aspire.CreateHostClient();

        using var response = await client.GetAsync(new Uri("/me", UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_token_from_the_realm_identifies_the_user()
    {
        using var client = await aspire.CreateHostClientAsync("developer");

        var me = await client.GetFromJsonAsync<Me>(new Uri("/me", UriKind.Relative), TestContext.Current.CancellationToken);

        me?.Name.ShouldBe("developer");
    }

    [Fact]
    public async Task Each_user_resolves_to_their_own_tenant()
    {
        using var first = await aspire.CreateHostClientAsync("developer");
        using var second = await aspire.CreateHostClientAsync("developer-b");

        var firstTenant = await first.GetFromJsonAsync<Tenant>(new Uri("/me/tenant", UriKind.Relative), TestContext.Current.CancellationToken);
        var secondTenant = await second.GetFromJsonAsync<Tenant>(new Uri("/me/tenant", UriKind.Relative), TestContext.Current.CancellationToken);

        firstTenant?.TenantId.ShouldBe("tenant-a");
        secondTenant?.TenantId.ShouldBe("tenant-b");
    }

    private sealed record Me(string? Name);

    private sealed record Tenant(string? TenantId);
}
