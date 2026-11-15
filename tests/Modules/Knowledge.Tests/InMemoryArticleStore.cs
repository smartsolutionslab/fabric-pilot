using FabricPilot.Modules.Knowledge.Application;
using FabricPilot.Modules.Knowledge.Application.DTOs;
using FabricPilot.Modules.Knowledge.Domain.Articles;

namespace FabricPilot.Modules.Knowledge.Tests;

/// <summary>The shared articles table, in memory; what is added counts as stored only once the unit of work commits.</summary>
internal sealed class InMemoryArticleStore : IArticleStore
{
    private readonly List<Article> pending = [];

    public List<Article> Saved { get; } = [];

    public void Add(Article article) => pending.Add(article);

    public Task<Option<Article>> FindAsync(ArticleId id, CancellationToken cancellationToken) => Task.FromResult(Find(id));

    public Task<Option<Article>> ReadAsync(ArticleId id, CancellationToken cancellationToken) => Task.FromResult(Find(id));

    public Task<PagedResult<ArticleSummaryDto>> ListAsync(Sorting<ArticleSortField> sorting, Page page, PageSize pageSize, CancellationToken cancellationToken) =>
        Task.FromResult(PageOf(Saved.AsQueryable(), sorting, page, pageSize));

    /// <summary>The database store matches with ILIKE; in memory, the same thing is an ordinal comparison without case.</summary>
    public Task<PagedResult<ArticleSummaryDto>> SearchPublishedAsync(string search, Sorting<ArticleSortField> sorting, Page page, PageSize pageSize, CancellationToken cancellationToken)
    {
        var term = search.Trim();
        var matching = Saved.AsQueryable().Published()
            .Where(article => term.Length == 0 || article.Title.Contains(term, StringComparison.OrdinalIgnoreCase) || article.Body.Contains(term, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(PageOf(matching, sorting, page, pageSize));
    }

    public void Commit()
    {
        Saved.AddRange(pending);
        pending.Clear();
    }

    private static PagedResult<ArticleSummaryDto> PageOf(IQueryable<Article> articles, Sorting<ArticleSortField> sorting, Page page, PageSize pageSize)
    {
        var items = articles
            .SortedBy(sorting)
            .Skip(page.Skip(pageSize))
            .Take(pageSize.Value)
            .Select(ArticleMappings.ToArticleSummaryDtoExpression());
        return new PagedResult<ArticleSummaryDto>([.. items], articles.Count(), page.Value, pageSize.Value);
    }

    private Option<Article> Find(ArticleId id) =>
        Saved.SingleOrDefault(candidate => candidate.Id == id) is { } article ? Option<Article>.Some(article) : Option<Article>.None;
}
