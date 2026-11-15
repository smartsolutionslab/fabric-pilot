using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Identity.Domain.TenantUsers;
using Microsoft.Extensions.Logging;

namespace FabricPilot.Modules.Identity.Application.Commands.InviteTenantAdmin;

/// <summary>
/// Staff belong to no tenant, so the tenant is the route's (docs/adr, staff write for a named tenant); it opens no
/// tenant scope and touches no table.
/// </summary>
public sealed class InviteTenantAdminHandler(ITenantUserDirectory directory, ILogger<InviteTenantAdminHandler> logger)
    : ICommandHandler<InviteTenantAdmin, Result<Guid, ApiError>>
{
    public async Task<Result<Guid, ApiError>> HandleAsync(InviteTenantAdmin command, CancellationToken cancellationToken)
    {
        Ensure.That(command).IsNotNull();

        var (tenant, email, firstName, lastName, caller) = command;
        var group = TenantGroup.For(tenant);
        if (!group.HasValue || !await directory.TenantExistsAsync(group.Value, cancellationToken))
        {
            return Failure(IdentityFailures.TenantNotFound(tenant));
        }

        var user = NewTenantUser.Create(email, firstName, lastName);
        if (user.IsFailure)
        {
            return Failure(IdentityFailures.From(user.Error));
        }

        return await TenantUserInvitation.InviteAsync(directory, logger, group.Value, user.Value, TenantRole.CustomerAdmin, caller, cancellationToken);
    }
}
