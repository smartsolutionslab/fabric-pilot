namespace FabricPilot.Modules.Identity.Domain.TenantUsers;

/// <summary>A user's id in Keycloak, which is the only store of users.</summary>
public sealed record TenantUserId(Guid Value);
