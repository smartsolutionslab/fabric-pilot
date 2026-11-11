using FabricPilot.Modules.Dashboards.Domain;

namespace FabricPilot.Modules.Dashboards.Application.DTOs;

public static class DashboardMappings
{
    public static DashboardDto ToDashboardDto(this Dashboard dashboard)
    {
        Ensure.That(dashboard).IsNotNull();
        return new DashboardDto(dashboard.Id, dashboard.Name, [.. dashboard.Widgets.Select(widget => widget.ToDto())]);
    }

    public static DashboardSummaryDto ToDashboardSummaryDto(this Dashboard dashboard)
    {
        Ensure.That(dashboard).IsNotNull();
        return new DashboardSummaryDto(dashboard.Id, dashboard.Name, dashboard.Widgets.Count);
    }
}
