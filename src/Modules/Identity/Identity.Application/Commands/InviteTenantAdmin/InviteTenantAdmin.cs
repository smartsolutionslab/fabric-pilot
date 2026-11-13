using FabricPilot.BuildingBlocks.Messaging;

namespace FabricPilot.Modules.Identity.Application.Commands.InviteTenantAdmin;

/// <summary>Staff invite an admin into the tenant the route names; answers the new user's id.</summary>
public sealed record InviteTenantAdmin(string Tenant, string Email, string FirstName, string LastName, Guid Caller)
    : ICommand<Result<Guid, ApiError>>;
