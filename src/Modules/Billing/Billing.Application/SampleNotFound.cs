using System.Net;

namespace FabricPilot.Modules.Billing.Application;

public sealed record SampleNotFound(Guid Id)
    : ApiError("billing.sample-not-found", $"There is no sample {Id}.", HttpStatusCode.NotFound);
