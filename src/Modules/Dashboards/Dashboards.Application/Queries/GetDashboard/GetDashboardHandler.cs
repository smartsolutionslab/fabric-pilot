using FabricPilot.BuildingBlocks.Identity;
using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Dashboards.Application.DTOs;

namespace FabricPilot.Modules.Dashboards.Application.Queries.GetDashboard;

public sealed class GetDashboardHandler(IDashboardStore dashboards, ICurrentUser user) : IQueryHandler<GetDashboard, Result<DashboardDto, ApiError>>
{
    public async Task<Result<DashboardDto, ApiError>> HandleAsync(GetDashboard query, CancellationToken cancellationToken)
    {
        Ensure.That(query).IsNotNull();

        if (user.Subject is not { } owner)
        {
            return Failure(DashboardsFailures.NotSignedIn());
        }

        var found = await dashboards.ReadAsync(query.Id, owner, cancellationToken);
        if (!found.HasValue)
        {
            return Failure(DashboardsFailures.NotFound(query.Id));
        }

        return Success(found.Value.ToDashboardDto());
    }
}
