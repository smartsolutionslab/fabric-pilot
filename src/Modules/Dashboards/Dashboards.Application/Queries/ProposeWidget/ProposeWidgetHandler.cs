using FabricPilot.BuildingBlocks.Dashboards;
using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Dashboards.Application.DTOs;

namespace FabricPilot.Modules.Dashboards.Application.Queries.ProposeWidget;

/// <summary>
/// Only a declared widget the caller may see is proposed, as a visual its kind can show, under its declared title: the
/// model's arguments are untrusted input. Saving the proposal is the user's, through the same checks as any save.
/// </summary>
public sealed class ProposeWidgetHandler(WidgetCatalog catalog, IWidgetAccess access) : IQueryHandler<ProposeWidget, Result<WidgetProposalDto, ApiError>>
{
    /// <summary>The longest key a dashboard's widget may have.</summary>
    public const int MaximumKeyLength = 64;

    private static readonly string[] ChartVisuals = ["bar", "line", "pie"];

    public async Task<Result<WidgetProposalDto, ApiError>> HandleAsync(ProposeWidget query, CancellationToken cancellationToken)
    {
        Ensure.That(query).IsNotNull();

        if (query.Source is null || !catalog.TryFind(query.Source, out var widget) || !await access.AllowsAsync(widget, cancellationToken))
        {
            return Failure(DashboardsFailures.UnknownWidget(query.Source ?? string.Empty));
        }

        var fits = widget.Kind == WidgetKind.Chart ? ChartVisuals.Contains(query.Visual, StringComparer.Ordinal) : query.Visual == "table";
        if (!fits)
        {
            return Failure(DashboardsFailures.InvalidWidget(widget.Id, widget.Kind == WidgetKind.Chart ? "shows a chart: bar, line or pie." : "shows a table: table."));
        }

        var replaces = query.Replaces.Match<string?>(key => key, () => null);
        if (replaces is not null && (string.IsNullOrWhiteSpace(replaces) || replaces.Length > MaximumKeyLength))
        {
            return Failure(DashboardsFailures.InvalidWidget(replaces, $"is not a key a dashboard's widget can have: 1 to {MaximumKeyLength} characters."));
        }

        return Success(widget.ToWidgetProposalDto(query.Visual, replaces));
    }
}
