using FabricPilot.BuildingBlocks.Dashboards;
using FabricPilot.BuildingBlocks.Identity;
using FabricPilot.BuildingBlocks.Messaging;

namespace FabricPilot.Modules.Dashboards.Application.Queries.CountWidgets;

public sealed class CountWidgetsHandler(IDashboardStore dashboards, ICurrentUser user) : IQueryHandler<CountWidgets, Result<ChartSeries, ApiError>>
{
    public async Task<Result<ChartSeries, ApiError>> HandleAsync(CountWidgets query, CancellationToken cancellationToken)
    {
        Ensure.That(query).IsNotNull();

        if (user.Subject is not { } owner)
        {
            return Failure(DashboardsFailures.NotSignedIn());
        }

        // All of them: a chart's query takes no page, so a chart that showed only the first would hide the rest for good.
        var owned = await dashboards.AllAsync(owner, cancellationToken);
        return Success(new ChartSeries([.. owned.Select(dashboard => dashboard.Name)], [new ChartValues("Widgets", [.. owned.Select(dashboard => (decimal)dashboard.Widgets.Count)])]));
    }
}
