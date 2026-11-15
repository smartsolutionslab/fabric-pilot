using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.BuildingBlocks.Modules;

namespace FabricPilot.Modules.Contracts.Application.Queries.CanManage;

/// <summary>Whether the caller may manage contracts: the endpoint that sends it requires the policy, so it answers only a holder.</summary>
public sealed record CanManage : IQuery<Result<PermissionDto, ApiError>>;
