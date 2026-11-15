using System.Net;

namespace FabricPilot.Modules.Knowledge.Application;

public sealed record InvalidQuestion(int MaximumLength)
    : ApiError("knowledge.invalid-question", $"A question needs text, at most {MaximumLength} characters of it.", HttpStatusCode.UnprocessableEntity);
