using FabricPilot.BuildingBlocks.Dashboards;
using FabricPilot.BuildingBlocks.Identity;
using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Dashboards.Domain;

namespace FabricPilot.Modules.Dashboards.Application.Commands.SaveDashboard;

public sealed class SaveDashboardHandler(IDashboardStore dashboards, WidgetCatalog catalog, IWidgetAccess access, ICurrentUser user)
    : ICommandHandler<SaveDashboard, Result<Guid, ApiError>>
{
    public const int MaximumNameLength = 100;

    public const int MaximumWidgets = 50;

    public const int Columns = 12;

    private static readonly string[] ChartVisuals = ["bar", "line", "pie"];

    public async Task<Result<Guid, ApiError>> HandleAsync(SaveDashboard command, CancellationToken cancellationToken)
    {
        Ensure.That(command).IsNotNull();

        if (user.Subject is not { } owner)
        {
            return Failure(DashboardsFailures.NotSignedIn());
        }

        var problem = await ProblemAsync(command, cancellationToken);
        if (problem is not null)
        {
            return Failure(problem);
        }

        var found = await dashboards.FindAsync(command.Id, owner, cancellationToken);
        var dashboard = found.GetOrDefault(new Dashboard { Id = command.Id, Owner = owner });
        if (!found.HasValue)
        {
            dashboards.Add(dashboard);
        }

        dashboard.Name = command.Name.Trim();
        dashboard.Widgets.Clear();
        foreach (var placement in command.Widgets)
        {
            dashboard.Widgets.Add(new DashboardWidget
            {
                Key = placement.Key,
                Source = placement.Source,
                Visual = placement.Visual,
                X = placement.X,
                Y = placement.Y,
                Width = placement.Width,
                Height = placement.Height,
            });
        }

        // A dashboard with this id that belongs to someone else is not found above; inserting it then fails on the
        // primary key and answers 409 already-exists, so no one replaces another user's dashboard.
        await dashboards.SaveAsync(cancellationToken);
        return Success(command.Id);
    }

    /// <summary>The first thing wrong with the request, or null; a widget must be one the modules declare for the caller.</summary>
    private async Task<ApiError?> ProblemAsync(SaveDashboard command, CancellationToken cancellationToken)
    {
        if (command.Id == Guid.Empty)
        {
            return DashboardsFailures.InvalidId();
        }

        if (string.IsNullOrWhiteSpace(command.Name) || command.Name.Trim().Length > MaximumNameLength)
        {
            return DashboardsFailures.InvalidName(MaximumNameLength);
        }

        if (command.Widgets is null || command.Widgets.Count > MaximumWidgets)
        {
            return DashboardsFailures.TooManyWidgets(MaximumWidgets);
        }

        HashSet<string> keys = [with(StringComparer.Ordinal)];
        foreach (var placement in command.Widgets)
        {
            if (placement is null || string.IsNullOrWhiteSpace(placement.Key) || placement.Key.Length > 64 || !keys.Add(placement.Key))
            {
                return DashboardsFailures.InvalidWidget(placement?.Key ?? string.Empty, "needs a key of 1 to 64 characters that no other widget on the dashboard has.");
            }

            if (placement.Source is null || !catalog.TryFind(placement.Source, out var widget) || !await access.AllowsAsync(widget, cancellationToken))
            {
                return DashboardsFailures.UnknownWidget(placement.Source ?? string.Empty);
            }

            var fits = widget.Kind == WidgetKind.Chart ? ChartVisuals.Contains(placement.Visual, StringComparer.Ordinal) : placement.Visual == "table";
            if (!fits)
            {
                return DashboardsFailures.InvalidWidget(placement.Key, widget.Kind == WidgetKind.Chart ? "shows a chart: bar, line or pie." : "shows a table: table.");
            }

            // Each bound on its own, never a sum: X + Width would wrap around for values near int.MaxValue.
            var outside = placement.X < 0 || placement.Width < 1 || placement.Width > Columns || placement.X > Columns - placement.Width
                || placement.Y < 0 || placement.Y > 1000 || placement.Height < 1 || placement.Height > 24;
            if (outside)
            {
                return DashboardsFailures.InvalidWidget(placement.Key, $"must lie within the grid's {Columns} columns, at most 24 rows high.");
            }
        }

        return null;
    }
}
