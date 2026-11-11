using FabricPilot.BuildingBlocks.Dashboards;

namespace FabricPilot.Modules.Dashboards.Application.DTOs;

public static class WidgetDefinitionMappings
{
    public static WidgetDescriptionDto ToDto(this WidgetDefinition widget)
    {
        Ensure.That(widget).IsNotNull();
        return new WidgetDescriptionDto(widget.Id, widget.Title, widget.Kind);
    }
}
