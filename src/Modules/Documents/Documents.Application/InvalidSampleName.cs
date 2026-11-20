using System.Net;
using FabricPilot.Modules.Documents.Domain.Samples;

namespace FabricPilot.Modules.Documents.Application;

public sealed record InvalidSampleName()
    : ApiError("documents.invalid-sample-name", $"A sample's name is 1 to {SampleName.MaxLength} characters and not blank.", HttpStatusCode.BadRequest);
