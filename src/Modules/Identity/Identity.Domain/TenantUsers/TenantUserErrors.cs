namespace FabricPilot.Modules.Identity.Domain.TenantUsers;

/// <summary>Every rule a tenant's user can break, each with its own code.</summary>
public static class TenantUserErrors
{
    public static TenantUserError RoleNotGrantable(string role) => new RoleNotGrantable(role);

    public static TenantUserError InvalidEmail(int maximum) => new InvalidEmail(maximum);

    public static TenantUserError InvalidName(int maximum) => new InvalidName(maximum);
}
