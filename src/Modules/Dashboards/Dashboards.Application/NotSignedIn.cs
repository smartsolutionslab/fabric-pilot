using System.Net;

namespace FabricPilot.Modules.Dashboards.Application;

public sealed record NotSignedIn() : ApiError("dashboards.not-signed-in", "A dashboard belongs to a signed-in user.", HttpStatusCode.Unauthorized);
