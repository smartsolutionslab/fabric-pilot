using FabricPilot.BuildingBlocks.Dashboards;
using FabricPilot.BuildingBlocks.Messaging;

namespace FabricPilot.Modules.Dashboards.Application.Queries.RunWidget;

/// <summary>Runs a declared widget's query as the caller; a table widget reads the given page.</summary>
public sealed record RunWidget(string Id, Page Page, PageSize PageSize) : IQuery<Result<WidgetData, ApiError>>;
