using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Knowledge.Application.DTOs;
using FabricPilot.Modules.Knowledge.Domain.Articles;

namespace FabricPilot.Modules.Knowledge.Application.Queries.GetArticle;

public sealed class GetArticleHandler(IArticleStore articles) : IQueryHandler<GetArticle, Result<ArticleDto, ApiError>>
{
    public async Task<Result<ArticleDto, ApiError>> HandleAsync(GetArticle query, CancellationToken cancellationToken)
    {
        Ensure.That(query).IsNotNull();

        var found = await articles.ReadAsync(new ArticleId(query.ArticleId), cancellationToken);
        if (!found.HasValue)
        {
            return Failure(KnowledgeFailures.ArticleNotFound(query.ArticleId));
        }

        return Success(found.Value.ToArticleDto());
    }
}
