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

    private readonly FakeEmbeddingsServer embeddings = new();

    private readonly FakeModelServer model;

    private DistributedApplication? app;

    public AspireFixture()
    {
        model = new FakeModelServer(embeddings.Endpoint);
    }

    /// <summary>The host's embeddings model: a stand-in on this machine, which a test can hold an answer back from.</summary>
    public FakeEmbeddingsServer Embeddings => embeddings;

    /// <summary>The host's chat model: a stand-in a test gives its answers; it hands every other request to <see cref="Embeddings"/>.</summary>
    public FakeModelServer Model => model;

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

        // A key-shaped value Azure does not accept: the integration tests never call the model.
        builder.Configuration["Parameters:azure-openai-api-key"] = "not-a-real-key";
        // Embeddings use the chat model's azure-openai-api-key, given its value above, and the endpoint the chat model shares:
        // the fixture's chat stand-in, which ends in /v1 and is used as is. It answers chat with the FakeChatClient a test
        // registered and passes every other request, embeddings included, on to the embeddings stand-in.
        builder.Configuration["Parameters:azure-openai-endpoint"] = model.Endpoint;

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

        await model.DisposeAsync();
        await embeddings.DisposeAsync();
    }

    private sealed record TokenResponse([property: JsonPropertyName("access_token")] string AccessToken);
}
