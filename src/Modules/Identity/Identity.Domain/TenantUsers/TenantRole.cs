namespace FabricPilot.Modules.Identity.Domain.TenantUsers;

/// <summary>
/// The realm roles a tenant's user may be given, by their admin or by staff: an allow-list, compared by exact name.
/// The service account can map any realm role (staff included), so this list is what confines it.
/// </summary>
public sealed record TenantRole
{
    private TenantRole(string name) => Name = name;

    public static TenantRole CustomerUser { get; } = new("customer-user");

    /// <summary>A composite of <see cref="CustomerUser"/> in the realm.</summary>
    public static TenantRole CustomerAdmin { get; } = new("customer-admin");

    public static IReadOnlyList<TenantRole> All { get; } = [CustomerUser, CustomerAdmin];

    public string Name { get; }

    public static Result<TenantRole, TenantUserError> From(string name)
    {
        var role = All.FirstOrDefault(candidate => string.Equals(candidate.Name, name, StringComparison.Ordinal));
        if (role is null)
        {
            return Failure(TenantUserErrors.RoleNotGrantable(name));
        }

        return Success(role);
    }
}
