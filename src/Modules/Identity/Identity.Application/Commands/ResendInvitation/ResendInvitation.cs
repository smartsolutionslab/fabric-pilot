using FabricPilot.BuildingBlocks.Messaging;

namespace FabricPilot.Modules.Identity.Application.Commands.ResendInvitation;

/// <summary>A customer admin has the invitation sent again to a user of their own tenant.</summary>
public sealed record ResendInvitation(Guid User, Guid Caller) : ICommand<Result<Unit, ApiError>>;
