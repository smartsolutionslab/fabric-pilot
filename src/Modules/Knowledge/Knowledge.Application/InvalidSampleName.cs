using System.Net;
using FabricPilot.Modules.Knowledge.Domain.Samples;

namespace FabricPilot.Modules.Knowledge.Application;

public sealed record InvalidSampleName()
    : ApiError("knowledge.invalid-sample-name", $"A sample's name is 1 to {SampleName.MaxLength} characters and not blank.", HttpStatusCode.BadRequest);
