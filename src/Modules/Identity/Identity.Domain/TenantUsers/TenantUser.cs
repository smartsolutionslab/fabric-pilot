namespace FabricPilot.Modules.Identity.Domain.TenantUsers;

/// <summary>
/// A user as Keycloak holds them: the full paths of their groups and every realm role they hold, directly or through a
/// composite.
/// </summary>
public sealed record TenantUser(
    TenantUserId Id,
    string Email,
    string FirstName,
    string LastName,
    IReadOnlyList<string> Groups,
    IReadOnlyList<string> RealmRoles)
{
    /// <summary>The allow-listed roles the user holds, in the allow-list's order.</summary>
    public IReadOnlyList<TenantRole> TenantRoles => [.. TenantRole.All.Where(Holds)];

    public bool Holds(TenantRole role)
    {
        Ensure.That(role).IsNotNull();
        return RealmRoles.Contains(role.Name, StringComparer.Ordinal);
    }
}
