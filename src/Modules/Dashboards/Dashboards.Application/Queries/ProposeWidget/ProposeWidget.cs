using System.ComponentModel;
using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Dashboards.Application.DTOs;

namespace FabricPilot.Modules.Dashboards.Application.Queries.ProposeWidget;

/// <summary>
/// The dashboards agent's proposal of one widget for the open dashboard: a declared widget as a fitting visual. It reads
/// the catalogue and saves nothing; the user accepts the proposal into the draft and saves it as any edit.
/// </summary>
public sealed record ProposeWidget(
    [property: Description("The id of a declared widget, as list_widgets lists it, e.g. dashboards.widget-counts.")]
    string Source,
    [property: Description("How to show it: bar, line or pie for a chart; table for a table.")]
    string Visual,
    [property: Description("The key of the open dashboard's widget this one takes the place of, or null for a new widget.")]
    Option<string> Replaces) : IQuery<Result<WidgetProposalDto, ApiError>>;
