using FabricPilot.Modules.Dashboards.Application.Commands.SaveDashboard;

namespace FabricPilot.Modules.Dashboards.Api.Endpoints;

/// <summary>The body of <c>PUT /dashboards/{id}</c>: the whole dashboard, which replaces what was saved.</summary>
public sealed record SaveDashboardRequest(string Name, IReadOnlyList<WidgetPlacement> Widgets);
