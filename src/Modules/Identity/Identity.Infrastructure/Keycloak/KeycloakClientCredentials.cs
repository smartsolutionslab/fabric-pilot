namespace FabricPilot.Modules.Identity.Infrastructure.Keycloak;

/// <summary>A confidential client's id and secret, with which its service account mints a token.</summary>
public sealed record KeycloakClientCredentials(string ClientId, string ClientSecret);
