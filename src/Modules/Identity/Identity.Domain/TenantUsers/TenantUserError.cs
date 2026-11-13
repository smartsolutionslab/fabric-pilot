namespace FabricPilot.Modules.Identity.Domain.TenantUsers;

/// <summary>The ways a tenant's user is refused: a new one fails the build until the Application maps it.</summary>
public union TenantUserError(RoleNotGrantable, InvalidEmail, InvalidName);
