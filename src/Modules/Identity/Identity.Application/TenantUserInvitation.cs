using FabricPilot.Modules.Identity.Domain.TenantUsers;
using Microsoft.Extensions.Logging;

namespace FabricPilot.Modules.Identity.Application;

/// <summary>Creating a user in a tenant with a role, for an admin of the tenant and for staff alike.</summary>
public static class TenantUserInvitation
{
    /// <summary>
    /// Creates the user in the group, grants the role and has the invitation sent. A failed grant deletes the user again
    /// and rethrows, so no member without a role is left behind; a failed email leaves the user invited and is logged,
    /// and the admin can have it sent again.
    /// </summary>
    public static async Task<Result<Guid, ApiError>> InviteAsync(
        ITenantUserDirectory directory,
        ILogger logger,
        TenantGroup group,
        NewTenantUser user,
        TenantRole role,
        Guid caller,
        CancellationToken cancellationToken)
    {
        Ensure.That(directory).IsNotNull();
        Ensure.That(group).IsNotNull();
        Ensure.That(role).IsNotNull();

        var created = await directory.CreateAsync(group, user, cancellationToken);
        if (!created.HasValue)
        {
            return Failure(IdentityFailures.UserAlreadyExists());
        }

        var id = created.Value;
        try
        {
            await directory.SetRoleAsync(id, role, cancellationToken);
        }
        catch (Exception exception)
        {
            IdentityAudit.InvitationUndone(logger, exception, id.Value, group.TenantId);
            await directory.DeleteAsync(id, CancellationToken.None);
            throw;
        }

        IdentityAudit.Invited(logger, caller, id.Value, group.TenantId, role.Name);
        if (!await directory.SendInvitationAsync(id, cancellationToken))
        {
            IdentityAudit.InvitationNotSent(logger, id.Value, group.TenantId);
        }

        return Success(id.Value);
    }
}
