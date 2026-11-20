using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Contracts.Application.DTOs;

namespace FabricPilot.Modules.Contracts.Application.Queries.GetSample;

/// <summary>One sample by its id; the empty Guid is no sample's.</summary>
public sealed record GetSample(Guid Id) : IQuery<Result<ContractsSampleDto, ApiError>>;
