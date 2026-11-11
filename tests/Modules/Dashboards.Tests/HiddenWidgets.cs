using FabricPilot.BuildingBlocks.Dashboards;

namespace FabricPilot.Modules.Dashboards.Tests;

/// <summary>A caller who may see every declared widget but these, as a widget's policy would decide.</summary>
internal sealed class HiddenWidgets(IReadOnlyList<string> hidden) : IWidgetAccess
{
    public Task<bool> AllowsAsync(WidgetDefinition widget, CancellationToken cancellationToken) =>
        Task.FromResult(!hidden.Contains(widget.Id, StringComparer.Ordinal));
}
