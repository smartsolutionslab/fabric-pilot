using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;

[assembly: AssemblyFixture(typeof(FabricPilot.Integration.Tests.AspireFixture))]

namespace FabricPilot.Integration.Tests;

/// <summary>The whole application, started once per test run on empty containers.</summary>
public sealed class AspireFixture : IAsyncLifetime
{
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromMinutes(5);

    private DistributedApplication? app;

    public DistributedApplication App => app ?? throw new InvalidOperationException("The application has not started.");

    public async ValueTask InitializeAsync()
    {
        using var timeout = new CancellationTokenSource(StartupTimeout);
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AppHost>(timeout.Token);

        // Tests start from empty containers: never a developer's data, never a password a volume remembers.
        foreach (var resource in builder.Resources)
        {
            foreach (var volume in resource.Annotations.OfType<ContainerMountAnnotation>().Where(mount => mount.Type == ContainerMountType.Volume).ToList())
            {
                resource.Annotations.Remove(volume);
            }
        }

        app = await builder.BuildAsync(timeout.Token);
        await app.StartAsync(timeout.Token);
        await app.ResourceNotifications.WaitForResourceHealthyAsync("host", timeout.Token);
    }

    public HttpClient CreateHostClient() => App.CreateHttpClient("host");

    /// <summary>A client calling the host as a realm user; local users' passwords equal their names.</summary>
    public async Task<HttpClient> CreateHostClientAsync(string user)
    {
        using var identity = new HttpClient { BaseAddress = App.GetEndpoint("keycloak", "http") };
        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = "fabric-pilot-web",
            ["username"] = user,
            ["password"] = user,
        });
        using var response = await identity.PostAsync(new Uri("/realms/fabric-pilot/protocol/openid-connect/token", UriKind.Relative), form);
        response.EnsureSuccessStatusCode();
        var token = await response.Content.ReadFromJsonAsync<TokenResponse>()
            ?? throw new InvalidOperationException("The realm returned no token.");

        var client = CreateHostClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        return client;
    }

    public async ValueTask DisposeAsync()
    {
        if (app is not null)
        {
            await app.DisposeAsync();
        }
    }

    private sealed record TokenResponse([property: JsonPropertyName("access_token")] string AccessToken);
}
