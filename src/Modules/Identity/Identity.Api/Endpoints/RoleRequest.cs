namespace FabricPilot.Modules.Identity.Api.Endpoints;

/// <summary>The tenant role to give a user: customer-user or customer-admin.</summary>
public sealed record RoleRequest(string Role);
