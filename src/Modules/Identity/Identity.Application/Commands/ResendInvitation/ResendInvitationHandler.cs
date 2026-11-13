using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.BuildingBlocks.Tenancy;
using Microsoft.Extensions.Logging;

namespace FabricPilot.Modules.Identity.Application.Commands.ResendInvitation;

public sealed class ResendInvitationHandler(ITenantContext tenant, ITenantUserDirectory directory, ILogger<ResendInvitationHandler> logger)
    : ICommandHandler<ResendInvitation, Result<Unit, ApiError>>
{
    public async Task<Result<Unit, ApiError>> HandleAsync(ResendInvitation command, CancellationToken cancellationToken)
    {
        Ensure.That(command).IsNotNull();

        var (user, caller) = command;
        var group = TenantMembers.CallersGroup(tenant);
        if (!group.HasValue)
        {
            return Failure(IdentityFailures.NoTenant());
        }

        var member = await TenantMembers.FindAsync(directory, group.Value, user, cancellationToken);
        if (!member.HasValue)
        {
            return Failure(IdentityFailures.UserNotFound(user));
        }

        if (!await directory.SendInvitationAsync(member.Value.Id, cancellationToken))
        {
            return Failure(IdentityFailures.InvitationNotSent(user));
        }

        IdentityAudit.InvitationResent(logger, caller, user, group.Value.TenantId);
        return Success(Unit.Value);
    }
}
