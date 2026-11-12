using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Knowledge.Application.DTOs;

namespace FabricPilot.Modules.Knowledge.Application.Queries.ListArticles;

public sealed class ListArticlesHandler(IArticleStore articles) : IQueryHandler<ListArticles, Result<PagedResult<ArticleSummaryDto>, ApiError>>
{
    public async Task<Result<PagedResult<ArticleSummaryDto>, ApiError>> HandleAsync(ListArticles query, CancellationToken cancellationToken)
    {
        Ensure.That(query).IsNotNull();

        var (page, pageSize, sorting) = query;
        ArticleOrdering.EnsureDeclared(sorting);

        return Success(await articles.ListAsync(sorting, page, pageSize, cancellationToken));
    }
}
