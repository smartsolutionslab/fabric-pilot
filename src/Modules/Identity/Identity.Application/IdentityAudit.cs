using Microsoft.Extensions.Logging;

namespace FabricPilot.Modules.Identity.Application;

/// <summary>
/// One line per change to a user: Keycloak's admin events name the service account, so this names the person who asked
/// for it.
/// </summary>
public static partial class IdentityAudit
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "{Caller} invited user {User} into tenant {Tenant} as {Role}")]
    public static partial void Invited(ILogger logger, Guid caller, Guid user, string tenant, string role);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "{Caller} gave user {User} of tenant {Tenant} the role {Role}")]
    public static partial void RoleChanged(ILogger logger, Guid caller, Guid user, string tenant, string role);

    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "{Caller} had the invitation sent again to user {User} of tenant {Tenant}")]
    public static partial void InvitationResent(ILogger logger, Guid caller, Guid user, string tenant);

    [LoggerMessage(EventId = 4, Level = LogLevel.Warning, Message = "The invitation to user {User} of tenant {Tenant} could not be sent; the user exists with their role")]
    public static partial void InvitationNotSent(ILogger logger, Guid user, string tenant);

    [LoggerMessage(EventId = 5, Level = LogLevel.Warning, Message = "Granting user {User} of tenant {Tenant} their role failed; the user is deleted again")]
    public static partial void InvitationUndone(ILogger logger, Exception exception, Guid user, string tenant);
}
