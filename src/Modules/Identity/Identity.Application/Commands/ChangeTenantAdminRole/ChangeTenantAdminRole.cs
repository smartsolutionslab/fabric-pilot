using FabricPilot.BuildingBlocks.Messaging;

namespace FabricPilot.Modules.Identity.Application.Commands.ChangeTenantAdminRole;

/// <summary>Staff give a user of the tenant the route names another tenant role: an admin demoted, a user promoted.</summary>
public sealed record ChangeTenantAdminRole(string Tenant, Guid User, string Role, Guid Caller) : ICommand<Result<Unit, ApiError>>;
