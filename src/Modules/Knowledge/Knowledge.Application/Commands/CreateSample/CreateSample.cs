using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Knowledge.Domain.Samples;

namespace FabricPilot.Modules.Knowledge.Application.Commands.CreateSample;

/// <summary>Creates a sample with this name and answers its id.</summary>
public sealed record CreateSample(SampleName Name) : ICommand<Result<Guid, ApiError>>;
