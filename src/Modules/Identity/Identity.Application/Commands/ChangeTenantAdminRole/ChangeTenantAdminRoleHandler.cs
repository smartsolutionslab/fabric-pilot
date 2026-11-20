using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Identity.Domain.TenantUsers;
using Microsoft.Extensions.Logging;

namespace FabricPilot.Modules.Identity.Application.Commands.ChangeTenantAdminRole;

public sealed class ChangeTenantAdminRoleHandler(ITenantUserDirectory directory, ILogger<ChangeTenantAdminRoleHandler> logger)
    : ICommandHandler<ChangeTenantAdminRole, Result<Unit, ApiError>>
{
    public async Task<Result<Unit, ApiError>> HandleAsync(ChangeTenantAdminRole command, CancellationToken cancellationToken)
    {
        Ensure.That(command).IsNotNull();

        var (tenant, user, roleName, caller) = command;
        var group = TenantGroup.For(tenant);
        if (!group.HasValue || !await directory.TenantExistsAsync(group.Value, cancellationToken))
        {
            return Failure(IdentityFailures.TenantNotFound(tenant));
        }

        var role = TenantRole.From(roleName);
        if (role.IsFailure)
        {
            return Failure(IdentityFailures.From(role.Error));
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
