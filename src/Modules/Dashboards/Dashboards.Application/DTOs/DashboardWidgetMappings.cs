using FabricPilot.Modules.Dashboards.Domain;

namespace FabricPilot.Modules.Dashboards.Application.DTOs;

public static class DashboardWidgetMappings
{
    public static DashboardWidgetDto ToDto(this DashboardWidget widget)
    {
        Ensure.That(widget).IsNotNull();
        return new DashboardWidgetDto(widget.Key, widget.Source, widget.Visual, widget.X, widget.Y, widget.Width, widget.Height);
    }
}
