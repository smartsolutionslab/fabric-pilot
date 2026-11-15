namespace FabricPilot.Modules.Identity.Api.Endpoints;

/// <summary>An admin for staff to invite into the tenant the route names; they are given customer-admin.</summary>
public sealed record AdminInvitationRequest(string Email, string FirstName, string LastName);
