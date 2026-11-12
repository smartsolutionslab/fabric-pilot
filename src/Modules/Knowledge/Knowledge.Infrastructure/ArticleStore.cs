using FabricPilot.Modules.Knowledge.Application;
using FabricPilot.Modules.Knowledge.Application.DTOs;
using FabricPilot.Modules.Knowledge.Domain.Articles;
using FabricPilot.Modules.Knowledge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FabricPilot.Modules.Knowledge.Infrastructure;

/// <summary>Articles are shared by every tenant, so no tenant filter narrows these reads; the queries decide who sees what.</summary>
public sealed class ArticleStore(KnowledgeDbContext context) : IArticleStore
{
    public void Add(Article article) => context.Add(article);

    public async Task<Option<Article>> FindAsync(ArticleId id, CancellationToken cancellationToken)
    {
        var article = await context.Set<Article>().SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        return article is null ? Option<Article>.None : Option<Article>.Some(article);
    }

    public async Task<Option<Article>> ReadAsync(ArticleId id, CancellationToken cancellationToken)
    {
        var article = await context.Set<Article>().AsNoTracking().SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        return article is null ? Option<Article>.None : Option<Article>.Some(article);
    }

    public Task<PagedResult<ArticleSummaryDto>> ListAsync(Sorting<ArticleSortField> sorting, Page page, PageSize pageSize, CancellationToken cancellationToken) =>
        PageAsync(context.Set<Article>().AsNoTracking(), sorting, page, pageSize, cancellationToken);

    public Task<PagedResult<ArticleSummaryDto>> SearchPublishedAsync(string search, Sorting<ArticleSortField> sorting, Page page, PageSize pageSize, CancellationToken cancellationToken) =>
        PageAsync(context.Set<Article>().AsNoTracking().PublishedMatching(search), sorting, page, pageSize, cancellationToken);

    private static async Task<PagedResult<ArticleSummaryDto>> PageAsync(
        IQueryable<Article> articles,
        Sorting<ArticleSortField> sorting,
        Page page,
        PageSize pageSize,
        CancellationToken cancellationToken)
    {
        Ensure.That(sorting).IsNotNull();
        Ensure.That(page).IsNotNull();
        Ensure.That(pageSize).IsNotNull();

        var total = await articles.CountAsync(cancellationToken);
        var items = await articles
            .SortedBy(sorting)
            .Skip(page.Skip(pageSize))
            .Take(pageSize.Value)
            .Select(ArticleMappings.ToArticleSummaryDtoExpression())
            .ToListAsync(cancellationToken);
        return new PagedResult<ArticleSummaryDto>(items, total, page.Value, pageSize.Value);
    }
}
