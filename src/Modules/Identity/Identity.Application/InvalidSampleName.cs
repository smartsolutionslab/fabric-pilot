using System.Net;
using FabricPilot.Modules.Identity.Domain.Samples;

namespace FabricPilot.Modules.Identity.Application;

public sealed record InvalidSampleName()
    : ApiError("identity.invalid-sample-name", $"A sample's name is 1 to {SampleName.MaxLength} characters and not blank.", HttpStatusCode.BadRequest);
