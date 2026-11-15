using FabricPilot.BuildingBlocks.Messaging;

namespace FabricPilot.Modules.Identity.Application.Commands.ChangeTenantUserRole;

/// <summary>A customer admin gives a user of their own tenant another tenant role; never their own.</summary>
public sealed record ChangeTenantUserRole(Guid User, string Role, Guid Caller) : ICommand<Result<Unit, ApiError>>;
