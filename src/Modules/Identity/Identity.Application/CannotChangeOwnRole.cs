using System.Net;

namespace FabricPilot.Modules.Identity.Application;

public sealed record CannotChangeOwnRole()
    : ApiError("identity.cannot-change-own-role", "Nobody changes their own role; ask another admin.", HttpStatusCode.Conflict);
