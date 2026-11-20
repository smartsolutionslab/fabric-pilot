using System.Net;

namespace FabricPilot.Modules.Notifications.Application;

public sealed record SampleNotFound(Guid Id)
    : ApiError("notifications.sample-not-found", $"There is no sample {Id}.", HttpStatusCode.NotFound);
