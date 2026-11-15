using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Contracts.Identity;
using FabricPilot.Modules.Identity.Application.DTOs;

namespace FabricPilot.Modules.Identity.Application.Queries.ListStaff;

/// <summary>Reads every staff member and pages them here: Keycloak pages a role's members but does not count them.</summary>
public sealed class ListStaffHandler(IStaffDirectory staff)
    : IQueryHandler<ListStaff, Result<PagedResult<StaffMemberDto>, ApiError>>
{
    public async Task<Result<PagedResult<StaffMemberDto>, ApiError>> HandleAsync(ListStaff query, CancellationToken cancellationToken)
    {
        Ensure.That(query).IsNotNull();

        var (page, pageSize) = query;
        var members = await staff.ListAsync(cancellationToken);
        var items = members.Skip(page.Skip(pageSize)).Take(pageSize.Value).Select(member => member.ToDto()).ToList();
        return Success(new PagedResult<StaffMemberDto>(items, members.Count, page.Value, pageSize.Value));
    }
}
