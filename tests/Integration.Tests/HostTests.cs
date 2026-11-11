using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace FabricPilot.Integration.Tests;

public class HostTests(AspireFixture aspire)
{
    private static readonly TimeSpan ResourceTimeout = TimeSpan.FromMinutes(2);

    private static readonly string[] UpdatePassword = ["UPDATE_PASSWORD"];

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
    public async Task A_token_from_the_identity_provider_identifies_the_user()
    {
        using var client = await aspire.CreateHostClientAsync("developer");

        var me = await client.GetFromJsonAsync<Me>(new Uri("/me", UriKind.Relative), TestContext.Current.CancellationToken);

        me?.Name.ShouldBe("developer");
    }

    [Fact]
    public async Task A_reset_password_email_from_the_realm_reaches_mailpit()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var identity = new HttpClient { BaseAddress = aspire.App.GetEndpoint("keycloak", "http") };
        identity.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await AdminTokenAsync(identity, cancellationToken));
        var users = await identity.GetFromJsonAsync<RealmUser[]>(new Uri("/admin/realms/fabric-pilot/users?username=developer&exact=true", UriKind.Relative), cancellationToken) ?? [];

        using var sent = await identity.PutAsJsonAsync(new Uri($"/admin/realms/fabric-pilot/users/{users.Single().Id}/execute-actions-email", UriKind.Relative), UpdatePassword, cancellationToken);

        sent.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var recipient = await RecipientAsync("developer@example.com", cancellationToken);
        recipient.ShouldBe("developer@example.com");
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

    /// <summary>A token of the master realm's administrator: "admin", with the password the AppHost generated for it.</summary>
    private async Task<string> AdminTokenAsync(HttpClient identity, CancellationToken cancellationToken)
    {
        var password = aspire.App.Services.GetRequiredService<DistributedApplicationModel>().Resources.OfType<ParameterResource>().Single(parameter => parameter.Name == "keycloak-password");
        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = "admin-cli",
            ["username"] = "admin",
            ["password"] = await password.GetValueAsync(cancellationToken) ?? string.Empty,
        });
        using var response = await identity.PostAsync(new Uri("/realms/master/protocol/openid-connect/token", UriKind.Relative), form, cancellationToken);
        response.EnsureSuccessStatusCode();
        var token = await response.Content.ReadFromJsonAsync<AdminToken>(cancellationToken);
        return token?.AccessToken ?? throw new InvalidOperationException("The master realm returned no token.");
    }

    /// <summary>The recipient of the first email Mailpit caught for the address, or an empty string when none arrives.</summary>
    private async Task<string> RecipientAsync(string address, CancellationToken cancellationToken)
    {
        using var mailpit = new HttpClient { BaseAddress = aspire.App.GetEndpoint("mailpit", "http") };
        for (var attempt = 0; attempt < 30; attempt++)
        {
            var inbox = await mailpit.GetFromJsonAsync<Inbox>(new Uri($"/api/v1/search?query=to:{address}", UriKind.Relative), cancellationToken);
            var recipient = inbox?.Messages.SelectMany(message => message.To).FirstOrDefault(to => to.Address == address);
            if (recipient is not null)
            {
                return recipient.Address;
            }

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }

        return string.Empty;
    }

    private sealed record Me(string? Name);

    private sealed record Tenant(string? TenantId);

    private sealed record AdminToken([property: JsonPropertyName("access_token")] string AccessToken);

    private sealed record RealmUser(string Id);

    private sealed record Inbox(IReadOnlyList<Message> Messages);

    private sealed record Message(IReadOnlyList<Recipient> To);

    private sealed record Recipient(string Address);
}
