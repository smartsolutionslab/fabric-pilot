using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Notifications.Domain.Samples;

namespace FabricPilot.Modules.Notifications.Application.Commands.CreateSample;

/// <summary>Creates a sample with this name and answers its id.</summary>
public sealed record CreateSample(SampleName Name) : ICommand<Result<Guid, ApiError>>;
