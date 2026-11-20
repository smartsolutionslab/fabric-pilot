using System.Net;

namespace FabricPilot.Modules.Knowledge.Application;

public sealed record SampleNotFound(Guid Id)
    : ApiError("knowledge.sample-not-found", $"There is no sample {Id}.", HttpStatusCode.NotFound);
