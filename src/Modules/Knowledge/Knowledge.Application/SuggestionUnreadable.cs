using System.Net;

namespace FabricPilot.Modules.Knowledge.Application;

/// <summary>The model answered, but not in the shape it was asked for: nothing of it is shown, and staff write the answer.</summary>
public sealed record SuggestionUnreadable()
    : ApiError("knowledge.suggestion-unreadable", "The model's answer could not be read, so no answer was suggested.", HttpStatusCode.BadGateway);
