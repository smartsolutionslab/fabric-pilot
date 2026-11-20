using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Identity.Application.DTOs;

namespace FabricPilot.Modules.Identity.Application.Queries.GetSample;

/// <summary>One sample by its id; the empty Guid is no sample's.</summary>
public sealed record GetSample(Guid Id) : IQuery<Result<IdentitySampleDto, ApiError>>;
