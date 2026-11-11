using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Dashboards.Application.DTOs;

namespace FabricPilot.Modules.Dashboards.Application.Queries.DescribeWidget;

/// <summary>One declared widget the caller may see, by its id: what a saved widget shows as its title, whatever page lists it.</summary>
public sealed record DescribeWidget(string Id) : IQuery<Result<WidgetDescriptionDto, ApiError>>;
