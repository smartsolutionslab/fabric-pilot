using FabricPilot.Modules.Identity.Domain.TenantUsers;

namespace FabricPilot.Modules.Identity.Application;

/// <summary>
/// The realm's users, through Keycloak's admin API: the only store this module has. It answers what Keycloak holds and
/// confines nothing; the handlers decide which tenant's users a caller may touch. An unreachable Keycloak throws.
/// </summary>
public interface ITenantUserDirectory
{
    /// <summary>Whether the realm has the tenant's group.</summary>
    Task<bool> TenantExistsAsync(TenantGroup group, CancellationToken cancellationToken);

    /// <summary>
    /// Creates the user as a member of the group in one call, so they are never outside it; enabled, with the email to
    /// verify and a password to set. None when the realm has a user with that username or email already.
    /// </summary>
    Task<Option<TenantUserId>> CreateAsync(TenantGroup group, NewTenantUser user, CancellationToken cancellationToken);

    /// <summary>The user with their groups and effective realm roles; None when the realm has no such user.</summary>
    Task<Option<TenantUser>> FindAsync(TenantUserId id, CancellationToken cancellationToken);

    /// <summary>The ids of the group's members ordered by username, at most the first 1,000.</summary>
    Task<IReadOnlyList<TenantUserId>> ListMembersAsync(TenantGroup group, CancellationToken cancellationToken);

    /// <summary>Grants the role, then removes the user's other allow-listed roles; roles outside the allow-list stay as they are.</summary>
    Task SetRoleAsync(TenantUserId id, TenantRole role, CancellationToken cancellationToken);

    /// <summary>Has Keycloak email the user to verify their address and set a password; false when it could not send.</summary>
    Task<bool> SendInvitationAsync(TenantUserId id, CancellationToken cancellationToken);

    /// <summary>Deletes the user: only to undo a creation whose role grant failed.</summary>
    Task DeleteAsync(TenantUserId id, CancellationToken cancellationToken);
}
