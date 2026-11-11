using System.Text.Json;
using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Dashboards.Application.Commands.SaveDashboard;
using FabricPilot.Modules.Dashboards.Application.Queries.ProposeWidget;

namespace FabricPilot.Modules.Dashboards.Application.Queries.TalkToTheDashboards;

/// <summary>
/// The open dashboard's draft from a run's state, untrusted input: read in shape and size only, and never saved. A
/// state without <c>widgets</c> is an empty draft; anything else that is not a list of widgets is refused.
/// </summary>
public static class DashboardDraft
{
    private const int MaximumSourceLength = 200;

    private const int MaximumVisualLength = 16;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private static readonly IReadOnlyList<DraftWidget> Empty = [];

    public static Result<IReadOnlyList<DraftWidget>, ApiError> Read(JsonElement? state)
    {
        if (state is not { } given || given.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            return Success(Empty);
        }

        if (given.ValueKind != JsonValueKind.Object)
        {
            return Failure(DashboardAgentFailures.InvalidDraft("is not an object."));
        }

        if (!given.TryGetProperty("widgets", out var widgets))
        {
            return Success(Empty);
        }

        if (widgets.ValueKind != JsonValueKind.Array || widgets.GetArrayLength() > SaveDashboardHandler.MaximumWidgets)
        {
            return Failure(DashboardAgentFailures.InvalidDraft($"has widgets that are not a list of at most {SaveDashboardHandler.MaximumWidgets}."));
        }

        List<DraftWidget> read = [];
        foreach (var widget in widgets.EnumerateArray())
        {
            var key = Text(widget, "key", ProposeWidgetHandler.MaximumKeyLength);
            var source = Text(widget, "source", MaximumSourceLength);
            var visual = Text(widget, "visual", MaximumVisualLength);
            if (key.Length == 0 || source.Length == 0 || visual.Length == 0)
            {
                return Failure(DashboardAgentFailures.InvalidDraft("has a widget without a key, a source and a visual, each a short text."));
            }

            read.Add(new DraftWidget(key, source, visual, string.Empty));
        }

        IReadOnlyList<DraftWidget> draft = read;
        return Success(draft);
    }

    /// <summary>The draft as the message the model reads before the conversation.</summary>
    public static string Describe(IReadOnlyList<DraftWidget> widgets) =>
        "The dashboard open in the browser shows these widgets, as JSON. A proposal that changes one of them names its key in replaces: "
        + JsonSerializer.Serialize(widgets, Json);

    /// <summary>The property's text when it is a string of 1 to <paramref name="maximum"/> characters, not blank; otherwise empty.</summary>
    private static string Text(JsonElement widget, string name, int maximum)
    {
        if (widget.ValueKind != JsonValueKind.Object || !widget.TryGetProperty(name, out var property) || property.ValueKind != JsonValueKind.String)
        {
            return string.Empty;
        }

        var text = property.GetString() ?? string.Empty;
        return string.IsNullOrWhiteSpace(text) || text.Length > maximum ? string.Empty : text;
    }
}
