using System.Net;

namespace FabricPilot.Modules.Assistant.Application;

public sealed record NothingToCount() : ApiError("assistant.nothing-to-count", "There are no words to count.", HttpStatusCode.BadRequest);
