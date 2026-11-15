namespace FabricPilot.Modules.Identity.Domain.TenantUsers;

public sealed record RoleNotGrantable(string Role)
    : DomainError("identity.role-not-grantable", $"'{Role}' is not a role a tenant's user can be given; use customer-user or customer-admin.");
