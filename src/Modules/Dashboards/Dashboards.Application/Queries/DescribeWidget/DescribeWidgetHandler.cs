using FabricPilot.BuildingBlocks.Dashboards;
using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Dashboards.Application.DTOs;

namespace FabricPilot.Modules.Dashboards.Application.Queries.DescribeWidget;

/// <summary>A widget is described to the callers it is listed and run for, and to no one else: anything else is not found.</summary>
public sealed class DescribeWidgetHandler(WidgetCatalog catalog, IWidgetAccess access) : IQueryHandler<DescribeWidget, Result<WidgetDescriptionDto, ApiError>>
{
    public async Task<Result<WidgetDescriptionDto, ApiError>> HandleAsync(DescribeWidget query, CancellationToken cancellationToken)
    {
        Ensure.That(query).IsNotNull();

        if (!catalog.TryFind(query.Id, out var widget) || !await access.AllowsAsync(widget, cancellationToken))
        {
            return Failure(DashboardsFailures.WidgetNotFound(query.Id));
        }

        return Success(widget.ToDto());
    }
}
