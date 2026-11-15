using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.BuildingBlocks.Tenancy;
using FabricPilot.Modules.Identity.Domain.TenantUsers;
using Microsoft.Extensions.Logging;

namespace FabricPilot.Modules.Identity.Application.Commands.ChangeTenantUserRole;

public sealed class ChangeTenantUserRoleHandler(ITenantContext tenant, ITenantUserDirectory directory, ILogger<ChangeTenantUserRoleHandler> logger)
    : ICommandHandler<ChangeTenantUserRole, Result<Unit, ApiError>>
{
    public async Task<Result<Unit, ApiError>> HandleAsync(ChangeTenantUserRole command, CancellationToken cancellationToken)
    {
        Ensure.That(command).IsNotNull();

        var (user, roleName, caller) = command;
        var group = TenantMembers.CallersGroup(tenant);
        if (!group.HasValue)
        {
            return Failure(IdentityFailures.NoTenant());
        }

        var role = TenantRole.From(roleName);
        if (role.IsFailure)
        {
            return Failure(IdentityFailures.From(role.Error));
        }

        // An admin who demoted themselves could leave the tenant without one.
        if (user == caller)
        {
            return Failure(IdentityFailures.CannotChangeOwnRole());
        }

        var member = await TenantMembers.FindAsync(directory, group.Value, user, cancellationToken);
        if (!member.HasValue)
        {
            return Failure(IdentityFailures.UserNotFound(user));
        }

        await directory.SetRoleAsync(member.Value.Id, role.Value, cancellationToken);
        IdentityAudit.RoleChanged(logger, caller, user, group.Value.TenantId, role.Value.Name);
        return Success(Unit.Value);
    }
}
