using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Contracts.Domain.Samples;

namespace FabricPilot.Modules.Contracts.Application.Commands.CreateSample;

/// <summary>Creates a sample with this name and answers its id.</summary>
public sealed record CreateSample(SampleName Name) : ICommand<Result<Guid, ApiError>>;
