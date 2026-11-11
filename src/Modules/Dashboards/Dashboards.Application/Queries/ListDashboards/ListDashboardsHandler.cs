using FabricPilot.BuildingBlocks.Identity;
using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Dashboards.Application.DTOs;

namespace FabricPilot.Modules.Dashboards.Application.Queries.ListDashboards;

public sealed class ListDashboardsHandler(IDashboardStore dashboards, ICurrentUser user)
    : IQueryHandler<ListDashboards, Result<PagedResult<DashboardSummaryDto>, ApiError>>
{
    public async Task<Result<PagedResult<DashboardSummaryDto>, ApiError>> HandleAsync(ListDashboards query, CancellationToken cancellationToken)
    {
        Ensure.That(query).IsNotNull();

        if (user.Subject is not { } owner)
        {
            return Failure(DashboardsFailures.NotSignedIn());
        }

        var page = await dashboards.ListAsync(owner, query.Page, query.PageSize, cancellationToken);
        return Success(new PagedResult<DashboardSummaryDto>([.. page.Items.Select(dashboard => dashboard.ToDashboardSummaryDto())], page.TotalCount, page.Page, page.PageSize));
    }
}
