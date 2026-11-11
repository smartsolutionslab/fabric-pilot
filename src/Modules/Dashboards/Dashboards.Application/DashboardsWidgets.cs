using FabricPilot.BuildingBlocks.Dashboards;
using FabricPilot.Modules.Dashboards.Application.DTOs;
using FabricPilot.Modules.Dashboards.Application.Queries.CountWidgets;
using FabricPilot.Modules.Dashboards.Application.Queries.ListDashboards;

namespace FabricPilot.Modules.Dashboards.Application;

/// <summary>
/// The widgets this module offers, as an example of the shape: every module declares its own in one class like this,
/// and a dashboard can show nothing else.
/// </summary>
public sealed class DashboardsWidgets : IModuleWidgets
{
    public void Declare(WidgetCatalogBuilder widgets)
    {
        Ensure.That(widgets).IsNotNull();

        widgets
            .Table<ListDashboards, DashboardSummaryDto>("my-dashboards", "My dashboards", (page, pageSize) => new ListDashboards(page, pageSize))
            .Chart("widget-counts", "Widgets per dashboard", () => new CountWidgets());
    }
}
