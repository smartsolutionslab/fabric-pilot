using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Identity.Application.DTOs;
using FabricPilot.Modules.Identity.Domain.TenantUsers;

namespace FabricPilot.Modules.Identity.Application.Queries.ListTenantAdmins;

/// <summary>
/// Reads every member's roles to find the admins: tenants are small, and the members list stops at 1,000. Reading a
/// role's users instead would need the service account to view the realm.
/// </summary>
public sealed class ListTenantAdminsHandler(ITenantUserDirectory directory)
    : IQueryHandler<ListTenantAdmins, Result<PagedResult<TenantUserDto>, ApiError>>
{
    public async Task<Result<PagedResult<TenantUserDto>, ApiError>> HandleAsync(ListTenantAdmins query, CancellationToken cancellationToken)
    {
        Ensure.That(query).IsNotNull();

        var (tenant, page, pageSize) = query;
        var group = TenantGroup.For(tenant);
        if (!group.HasValue || !await directory.TenantExistsAsync(group.Value, cancellationToken))
        {
            return Failure(IdentityFailures.TenantNotFound(tenant));
        }

        List<TenantUser> admins = [];
        foreach (var member in await directory.ListMembersAsync(group.Value, cancellationToken))
        {
            var user = await directory.FindAsync(member, cancellationToken);
            if (user.HasValue && user.Value.Holds(TenantRole.CustomerAdmin))
            {
                admins.Add(user.Value);
            }
        }

        var items = admins.Skip(page.Skip(pageSize)).Take(pageSize.Value).Select(admin => admin.ToDto()).ToList();
        return Success(new PagedResult<TenantUserDto>(items, admins.Count, page.Value, pageSize.Value));
    }
}
