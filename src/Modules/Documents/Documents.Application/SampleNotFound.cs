using System.Net;

namespace FabricPilot.Modules.Documents.Application;

public sealed record SampleNotFound(Guid Id)
    : ApiError("documents.sample-not-found", $"There is no sample {Id}.", HttpStatusCode.NotFound);
