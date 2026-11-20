using System.Net;

namespace FabricPilot.Modules.Contracts.Application;

public sealed record SampleNotFound(Guid Id)
    : ApiError("contracts.sample-not-found", $"There is no sample {Id}.", HttpStatusCode.NotFound);
