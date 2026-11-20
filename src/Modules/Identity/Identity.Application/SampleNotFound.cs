using System.Net;

namespace FabricPilot.Modules.Identity.Application;

public sealed record SampleNotFound(Guid Id)
    : ApiError("identity.sample-not-found", $"There is no sample {Id}.", HttpStatusCode.NotFound);
