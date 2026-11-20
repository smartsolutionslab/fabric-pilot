using System.Net;

namespace FabricPilot.Modules.Tickets.Application;

public sealed record SampleNotFound(Guid Id)
    : ApiError("tickets.sample-not-found", $"There is no sample {Id}.", HttpStatusCode.NotFound);
