using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.BuildingBlocks.Tenancy;
using FabricPilot.Modules.Identity.Application.DTOs;

namespace FabricPilot.Modules.Identity.Application.Queries.ListTenantUsers;

/// <summary>
/// Keycloak pages a group's members but does not count them, so the handler reads the ids (up to 1,000) and pages them
/// itself, then reads each user on the page.
/// </summary>
public sealed class ListTenantUsersHandler(ITenantContext tenant, ITenantUserDirectory directory)
    : IQueryHandler<ListTenantUsers, Result<PagedResult<TenantUserDto>, ApiError>>
{
    public async Task<Result<PagedResult<TenantUserDto>, ApiError>> HandleAsync(ListTenantUsers query, CancellationToken cancellationToken)
    {
        Ensure.That(query).IsNotNull();

        var (page, pageSize) = query;
        var group = TenantMembers.CallersGroup(tenant);
        if (!group.HasValue)
        {
            return Failure(IdentityFailures.NoTenant());
        }

        var members = await directory.ListMembersAsync(group.Value, cancellationToken);
        List<TenantUserDto> items = [];
        foreach (var member in members.Skip(page.Skip(pageSize)).Take(pageSize.Value))
        {
            var user = await directory.FindAsync(member, cancellationToken);
            if (user.HasValue)
            {
                items.Add(user.Value.ToDto());
            }
        }

        return Success(new PagedResult<TenantUserDto>(items, members.Count, page.Value, pageSize.Value));
    }
}
