using FabricPilot.BuildingBlocks.Api;
using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Knowledge.Application;
using FabricPilot.Modules.Knowledge.Application.DTOs;
using FabricPilot.Modules.Knowledge.Application.Queries.GetPublishedArticle;
using FabricPilot.Modules.Knowledge.Application.Queries.SearchArticles;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace FabricPilot.Modules.Knowledge.Api.Endpoints;

/// <summary>
/// The published articles, for every signed-in user of every tenant, and for staff: searching them and reading one.
/// The module group's authentication is all they need; which articles they see is the queries' rule, published only.
/// </summary>
public sealed class ArticleEndpoints : IEndpoint
{
    public void Map(IEndpointRouteBuilder endpoints)
    {
        Ensure.That(endpoints).IsNotNull();

        // One OpenAPI tag for the module's endpoints: the generated client refetches what a tag provides when a request
        // with the same tag changes it, so a published article shows up in the search.
        var articles = endpoints.MapGroup("/articles").WithTags("Knowledge");

        articles.MapGet(string.Empty, SearchAsync);
        articles.MapGet("/{articleId:guid}", async (Guid articleId, IDispatcher dispatcher, CancellationToken cancellationToken) =>
            (await dispatcher.QueryAsync(new GetPublishedArticle(articleId), cancellationToken)).ToHttpResult());
    }

    /// <summary>Every published article without a term; the latest change first unless the caller names a field.</summary>
    private static async Task<Results<Ok<PagedResult<ArticleSummaryDto>>, ProblemHttpResult>> SearchAsync(
        string? search,
        Page? page,
        PageSize? pageSize,
        ArticleSortField? sortBy,
        SortDirection? sortDirection,
        IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var sorting = new Sorting<ArticleSortField>(
            sortBy ?? SearchArticles.LatestFirst.Field,
            sortDirection ?? SearchArticles.LatestFirst.Direction);
        var searchArticles = new SearchArticles(search ?? string.Empty, page ?? Page.First, pageSize ?? PageSize.Default, sorting);
        return (await dispatcher.QueryAsync(searchArticles, cancellationToken)).ToHttpResult();
    }
}
