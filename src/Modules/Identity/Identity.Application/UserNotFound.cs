using System.Net;

namespace FabricPilot.Modules.Identity.Application;

public sealed record UserNotFound(Guid User)
    : ApiError("identity.user-not-found", $"There is no user {User} in this tenant.", HttpStatusCode.NotFound);
