using System.Net;

namespace FabricPilot.Modules.Knowledge.Application;

public sealed record ArticleNotFound(Guid Article)
    : ApiError("knowledge.article-not-found", $"There is no article {Article}.", HttpStatusCode.NotFound);
