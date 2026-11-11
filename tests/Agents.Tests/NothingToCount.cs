using System.Net;

namespace FabricPilot.Agents.Tests;

public sealed record NothingToCount() : ApiError("samples.nothing-to-count", "There are no words to count.", HttpStatusCode.BadRequest);
