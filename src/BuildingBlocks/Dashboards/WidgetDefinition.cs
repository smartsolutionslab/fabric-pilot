using FabricPilot.BuildingBlocks.Messaging;

namespace FabricPilot.BuildingBlocks.Dashboards;

/// <summary>A declared widget, and how to run its query through the dispatcher as the caller.</summary>
/// <param name="Id"><c>&lt;module&gt;.&lt;key&gt;</c>, e.g. <c>billing.monthly-revenue</c>; what a saved dashboard names.</param>
/// <param name="Title">What the widget shows, for people.</param>
/// <param name="Kind">Whether the query answers a chart or a table.</param>
/// <param name="Query">The query record the widget runs.</param>
/// <param name="Result">What the query's handler returns.</param>
/// <param name="Policy">The policy a caller must satisfy to see or run the widget, if any beyond being signed in.</param>
/// <param name="Run">Builds the query, sends it through the dispatcher and turns its answer into <see cref="WidgetData"/>.</param>
public sealed record WidgetDefinition(
    string Id,
    string Title,
    WidgetKind Kind,
    Type Query,
    Type Result,
    Option<string> Policy,
    Func<IDispatcher, Page, PageSize, CancellationToken, Task<Result<WidgetData, ApiError>>> Run);
