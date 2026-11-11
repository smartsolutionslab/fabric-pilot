using FabricPilot.BuildingBlocks.Dashboards;
using FabricPilot.BuildingBlocks.Messaging;

namespace FabricPilot.Modules.Dashboards.Application.Queries.CountWidgets;

/// <summary>How many widgets each of the caller's dashboards holds: the sample chart widget.</summary>
public sealed record CountWidgets : IQuery<Result<ChartSeries, ApiError>>;
