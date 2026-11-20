using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Notifications.Application.DTOs;

namespace FabricPilot.Modules.Notifications.Application.Queries.ListSamples;

/// <summary>One page of the module's samples, by name.</summary>
public sealed record ListSamples(Page Page, PageSize PageSize) : IQuery<Result<PagedResult<NotificationsSampleDto>, ApiError>>;
