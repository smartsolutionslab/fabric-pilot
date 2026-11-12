using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Knowledge.Application.DTOs;
using FabricPilot.Modules.Knowledge.Domain.Articles;

namespace FabricPilot.Modules.Knowledge.Application.Queries.GetPublishedArticle;

public sealed class GetPublishedArticleHandler(IArticleStore articles) : IQueryHandler<GetPublishedArticle, Result<ArticleDto, ApiError>>
{
    public async Task<Result<ArticleDto, ApiError>> HandleAsync(GetPublishedArticle query, CancellationToken cancellationToken)
    {
        Ensure.That(query).IsNotNull();

        var found = await articles.ReadAsync(new ArticleId(query.ArticleId), cancellationToken);

        // A draft answers as an article that does not exist: a customer learns nothing of what staff have not published.
        if (!found.HasValue || found.Value.State != ArticleState.Published)
        {
            return Failure(KnowledgeFailures.ArticleNotFound(query.ArticleId));
        }

        return Success(found.Value.ToArticleDto());
    }
}
