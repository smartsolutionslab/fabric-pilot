using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.BuildingBlocks.Modules;

namespace FabricPilot.Modules.Identity.Application.Queries.CanManage;

/// <summary>Names the permission the caller holds; the policy on the endpoint has already refused anyone else.</summary>
public sealed class CanManageHandler : IQueryHandler<CanManage, Result<PermissionDto, ApiError>>
{
    public Task<Result<PermissionDto, ApiError>> HandleAsync(CanManage query, CancellationToken cancellationToken)
    {
        Ensure.That(query).IsNotNull();

        return Task.FromResult<Result<PermissionDto, ApiError>>(Success(IdentityPolicyNames.Manage.ToDto()));
    }
}
