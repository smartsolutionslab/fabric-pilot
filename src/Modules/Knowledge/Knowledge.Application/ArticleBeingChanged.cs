using System.Net;

namespace FabricPilot.Modules.Knowledge.Application;

public sealed record ArticleBeingChanged(Guid Article)
    : ApiError("knowledge.article-being-changed", $"The article {Article} is being changed by another request; try again once it is done.", HttpStatusCode.Conflict);
