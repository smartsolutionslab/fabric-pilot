using System.Net;
using FabricPilot.Modules.Notifications.Domain.Samples;

namespace FabricPilot.Modules.Notifications.Application;

public sealed record InvalidSampleName()
    : ApiError("notifications.invalid-sample-name", $"A sample's name is 1 to {SampleName.MaxLength} characters and not blank.", HttpStatusCode.BadRequest);
