using System.Net.Http.Headers;

namespace FabricPilot.Modules.Identity.Infrastructure.Keycloak;

/// <summary>Adds the service account's bearer token to every admin API request, retries included.</summary>
public sealed class KeycloakAdminAuthentication(KeycloakAdminToken token) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Ensure.That(request).IsNotNull();

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await token.GetAsync(cancellationToken));
        return await base.SendAsync(request, cancellationToken);
    }
}
