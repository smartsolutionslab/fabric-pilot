using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace FabricPilot.Modules.Identity.Infrastructure.Keycloak;

/// <summary>
/// A client's service-account access token (client credentials), minted on first use and kept until shortly before it
/// expires, for every admin API call the host makes as that client. One per client.
/// </summary>
public sealed class KeycloakAdminToken(IHttpClientFactory clients, KeycloakAdminSettings settings, KeycloakClientCredentials credentials, TimeProvider time) : IDisposable
{
    /// <summary>The named HttpClient that mints the token.</summary>
    public const string ClientName = "keycloak-admin-token";

    /// <summary>A token this close to its expiry is minted again, so none expires on its way to Keycloak.</summary>
    private static readonly TimeSpan Margin = TimeSpan.FromSeconds(30);

    private readonly SemaphoreSlim gate = new(1, 1);

    private string? token;

    private DateTimeOffset renewAt;

    public async Task<string> GetAsync(CancellationToken cancellationToken)
    {
        if (Current() is { } current)
        {
            return current;
        }

        await gate.WaitAsync(cancellationToken);
        try
        {
            if (Current() is { } minted)
            {
                return minted;
            }

            using var client = clients.CreateClient(ClientName);
            using var form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = credentials.ClientId,
                ["client_secret"] = credentials.ClientSecret,
            });
            using var response = await client.PostAsync(settings.TokenEndpoint, form, cancellationToken);
            response.EnsureSuccessStatusCode();
            var answer = await response.Content.ReadFromJsonAsync<TokenAnswer>(cancellationToken)
                ?? throw new InvalidOperationException("Keycloak answered the client credentials grant without a token.");

            token = answer.AccessToken;
            renewAt = time.GetUtcNow() + TimeSpan.FromSeconds(answer.ExpiresIn) - Margin;
            return token;
        }
        finally
        {
            gate.Release();
        }
    }

    public void Dispose() => gate.Dispose();

    private string? Current() => token is not null && time.GetUtcNow() < renewAt ? token : null;

    private sealed record TokenAnswer(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);
}
