namespace FabricPilot.Modules.Identity.Api.Endpoints;

/// <summary>A user to invite into the caller's tenant. A tenant in the body is not read: the caller's token names it.</summary>
public sealed record InvitationRequest(string Email, string FirstName, string LastName, string Role);
