using FabricPilot.BuildingBlocks.Dashboards;

namespace FabricPilot.Modules.Dashboards.Application.DTOs;

public static class WidgetProposalMappings
{
    /// <summary>The declared widget as a proposal, under its declared title: the model chooses the widget, not its name.</summary>
    public static WidgetProposalDto ToWidgetProposalDto(this WidgetDefinition widget, string visual, string? replaces)
    {
        Ensure.That(widget).IsNotNull();

        return new WidgetProposalDto(widget.Id, widget.Title, visual, replaces);
    }
}
