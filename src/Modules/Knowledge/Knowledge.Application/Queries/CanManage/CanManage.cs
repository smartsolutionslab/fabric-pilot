using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.BuildingBlocks.Modules;

namespace FabricPilot.Modules.Knowledge.Application.Queries.CanManage;

/// <summary>Whether the caller may manage knowledge: the endpoint that sends it requires the policy, so it answers only a holder.</summary>
public sealed record CanManage : IQuery<Result<PermissionDto, ApiError>>;
