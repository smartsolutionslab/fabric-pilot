using FabricPilot.BuildingBlocks.Api;
using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Knowledge.Application;
using FabricPilot.Modules.Knowledge.Application.Commands.EditArticle;
using FabricPilot.Modules.Knowledge.Application.Commands.PublishArticle;
using FabricPilot.Modules.Knowledge.Application.Commands.WriteArticle;
using FabricPilot.Modules.Knowledge.Application.DTOs;
using FabricPilot.Modules.Knowledge.Application.Queries.GetArticle;
using FabricPilot.Modules.Knowledge.Application.Queries.ListArticles;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace FabricPilot.Modules.Knowledge.Api.Endpoints;

/// <summary>
/// The operator's staff write, edit and publish the knowledge base every tenant reads, and read its drafts. Every write
/// runs inside <c>WritesSharedRecords()</c>, where articles and their chunks are saved for no tenant and so for all.
/// </summary>
public sealed class ManageArticleEndpoints : IEndpoint
{
    public void Map(IEndpointRouteBuilder endpoints)
    {
        Ensure.That(endpoints).IsNotNull();

        var articles = endpoints.MapGroup("/articles").WithTags("Knowledge");

        articles.MapPost(string.Empty, WriteAsync)
            .RequireAuthorization(KnowledgePolicyNames.WriteSharedRecords)
            .WritesSharedRecords();
        articles.MapPut("/{articleId:guid}", EditAsync)
            .RequireAuthorization(KnowledgePolicyNames.WriteSharedRecords)
            .WritesSharedRecords();
        articles.MapPost("/{articleId:guid}/publish", async (Guid articleId, IDispatcher dispatcher, CancellationToken cancellationToken) =>
                (await dispatcher.SendAsync(new PublishArticle(articleId), cancellationToken)).ToHttpResult(_ => TypedResults.NoContent()))
            .RequireAuthorization(KnowledgePolicyNames.WriteSharedRecords)
            .WritesSharedRecords();

        // The staff who write the base read its drafts too; nobody else does, so the same policy guards the reads.
        articles.MapGet("/manage", ListAsync)
            .RequireAuthorization(KnowledgePolicyNames.WriteSharedRecords);
        articles.MapGet("/manage/{articleId:guid}", async (Guid articleId, IDispatcher dispatcher, CancellationToken cancellationToken) =>
                (await dispatcher.QueryAsync(new GetArticle(articleId), cancellationToken)).ToHttpResult())
            .RequireAuthorization(KnowledgePolicyNames.WriteSharedRecords);
    }

    private static async Task<Results<Created<Guid>, ProblemHttpResult>> WriteAsync(ArticleContent content, IDispatcher dispatcher, CancellationToken cancellationToken)
    {
        Ensure.That(content).IsNotNull();

        var result = await dispatcher.SendAsync(new WriteArticle(content.Title, content.Body), cancellationToken);
        return result.ToHttpResult(id => TypedResults.Created($"/knowledge/articles/manage/{id}", id));
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> EditAsync(Guid articleId, ArticleContent content, IDispatcher dispatcher, CancellationToken cancellationToken)
    {
        Ensure.That(content).IsNotNull();

        var result = await dispatcher.SendAsync(new EditArticle(articleId, content.Title, content.Body), cancellationToken);
        return result.ToHttpResult(_ => TypedResults.NoContent());
    }

    /// <summary>Every article, drafts included; the latest change first unless the caller names a field.</summary>
    private static async Task<Results<Ok<PagedResult<ArticleSummaryDto>>, ProblemHttpResult>> ListAsync(
        Page? page,
        PageSize? pageSize,
        ArticleSortField? sortBy,
        SortDirection? sortDirection,
        IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var sorting = new Sorting<ArticleSortField>(
            sortBy ?? ListArticles.LatestFirst.Field,
            sortDirection ?? ListArticles.LatestFirst.Direction);
        var listArticles = new ListArticles(page ?? Page.First, pageSize ?? PageSize.Default, sorting);
        return (await dispatcher.QueryAsync(listArticles, cancellationToken)).ToHttpResult();
    }
}
