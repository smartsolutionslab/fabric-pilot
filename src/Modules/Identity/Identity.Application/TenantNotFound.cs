using System.Net;

namespace FabricPilot.Modules.Identity.Application;

public sealed record TenantNotFound(string Tenant)
    : ApiError("identity.tenant-not-found", $"There is no tenant '{Tenant}'.", HttpStatusCode.NotFound);
