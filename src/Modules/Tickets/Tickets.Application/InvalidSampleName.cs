using System.Net;
using FabricPilot.Modules.Tickets.Domain.Samples;

namespace FabricPilot.Modules.Tickets.Application;

public sealed record InvalidSampleName()
    : ApiError("tickets.invalid-sample-name", $"A sample's name is 1 to {SampleName.MaxLength} characters and not blank.", HttpStatusCode.BadRequest);
