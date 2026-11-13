using System.Net;

namespace FabricPilot.Modules.Identity.Application;

public sealed record UserAlreadyExists()
    : ApiError("identity.user-already-exists", "A user with this email address exists already.", HttpStatusCode.Conflict);
