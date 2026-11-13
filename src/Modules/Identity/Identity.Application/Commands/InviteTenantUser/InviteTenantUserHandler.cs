using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.BuildingBlocks.Tenancy;
using FabricPilot.Modules.Identity.Domain.TenantUsers;
using Microsoft.Extensions.Logging;

namespace FabricPilot.Modules.Identity.Application.Commands.InviteTenantUser;

/// <summary>The tenant is the caller's, from their token; nothing in the request names one.</summary>
public sealed class InviteTenantUserHandler(ITenantContext tenant, ITenantUserDirectory directory, ILogger<InviteTenantUserHandler> logger)
    : ICommandHandler<InviteTenantUser, Result<Guid, ApiError>>
{
    public async Task<Result<Guid, ApiError>> HandleAsync(InviteTenantUser command, CancellationToken cancellationToken)
    {
        Ensure.That(command).IsNotNull();

        var (email, firstName, lastName, roleName, caller) = command;
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

        var user = NewTenantUser.Create(email, firstName, lastName);
        if (user.IsFailure)
        {
            return Failure(IdentityFailures.From(user.Error));
        }

        return await TenantUserInvitation.InviteAsync(directory, logger, group.Value, user.Value, role.Value, caller, cancellationToken);
    }
}
