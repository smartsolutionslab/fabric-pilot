using System.Net;

namespace FabricPilot.Modules.Identity.Application;

public sealed record NoTenant()
    : ApiError("identity.no-tenant", "Only a user who belongs to exactly one tenant manages its users.", HttpStatusCode.Forbidden);
