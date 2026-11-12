using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Knowledge.Application.DTOs;

namespace FabricPilot.Modules.Knowledge.Application.Queries.SearchArticles;

public sealed class SearchArticlesHandler(IArticleStore articles) : IQueryHandler<SearchArticles, Result<PagedResult<ArticleSummaryDto>, ApiError>>
{
    public async Task<Result<PagedResult<ArticleSummaryDto>, ApiError>> HandleAsync(SearchArticles query, CancellationToken cancellationToken)
    {
        Ensure.That(query).IsNotNull();

        var (search, page, pageSize, sorting) = query;
        ArticleOrdering.EnsureDeclared(sorting);

        return Success(await articles.SearchPublishedAsync(search, sorting, page, pageSize, cancellationToken));
    }
}
