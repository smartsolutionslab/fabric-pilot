using FabricPilot.BuildingBlocks.Dashboards;
using FabricPilot.BuildingBlocks.Messaging;

namespace FabricPilot.Modules.Dashboards.Application.Queries.RunWidget;

/// <summary>
/// Only a declared widget the caller may see runs, through the dispatcher, in this request: its query sees the caller's
/// tenant and identity as it would behind its own endpoint. Anything else is not found, so no other query is reachable.
/// </summary>
public sealed class RunWidgetHandler(WidgetCatalog catalog, IWidgetAccess access, IDispatcher dispatcher) : IQueryHandler<RunWidget, Result<WidgetData, ApiError>>
{
    public async Task<Result<WidgetData, ApiError>> HandleAsync(RunWidget query, CancellationToken cancellationToken)
    {
        Ensure.That(query).IsNotNull();

        if (!catalog.TryFind(query.Id, out var widget) || !await access.AllowsAsync(widget, cancellationToken))
        {
            return Failure(DashboardsFailures.WidgetNotFound(query.Id));
        }

        return await widget.Run(dispatcher, query.Page, query.PageSize, cancellationToken);
    }
}
