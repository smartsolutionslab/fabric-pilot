using System.Net;
using FabricPilot.Modules.Billing.Domain.Samples;

namespace FabricPilot.Modules.Billing.Application;

public sealed record InvalidSampleName()
    : ApiError("billing.invalid-sample-name", $"A sample's name is 1 to {SampleName.MaxLength} characters and not blank.", HttpStatusCode.BadRequest);
