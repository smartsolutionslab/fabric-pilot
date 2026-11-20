using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Documents.Domain.Samples;

namespace FabricPilot.Modules.Documents.Application.Commands.CreateSample;

/// <summary>Creates a sample with this name and answers its id.</summary>
public sealed record CreateSample(SampleName Name) : ICommand<Result<Guid, ApiError>>;
