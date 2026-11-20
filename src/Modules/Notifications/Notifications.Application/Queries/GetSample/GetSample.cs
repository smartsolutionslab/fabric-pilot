using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Notifications.Application.DTOs;

namespace FabricPilot.Modules.Notifications.Application.Queries.GetSample;

/// <summary>One sample by its id; the empty Guid is no sample's.</summary>
public sealed record GetSample(Guid Id) : IQuery<Result<NotificationsSampleDto, ApiError>>;
