using FabricPilot.BuildingBlocks.Messaging;

namespace FabricPilot.Modules.Identity.Application.Commands.InviteTenantUser;

/// <summary>A customer admin invites a user into their own tenant with a tenant role; answers the new user's id.</summary>
public sealed record InviteTenantUser(string Email, string FirstName, string LastName, string Role, Guid Caller)
    : ICommand<Result<Guid, ApiError>>;
