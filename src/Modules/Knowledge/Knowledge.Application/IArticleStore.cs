using FabricPilot.Modules.Knowledge.Application.DTOs;
using FabricPilot.Modules.Knowledge.Domain.Articles;

namespace FabricPilot.Modules.Knowledge.Application;

/// <summary>
/// The articles table, shared by every tenant: every reader sees every row, so which articles a customer may see is the
/// queries' rule. Implemented in the module's Infrastructure project; changes are saved by the unit of work.
/// </summary>
public interface IArticleStore
{
    void Add(Article article);

    /// <summary>The article in either state, tracked: a command's changes to it are saved when the unit of work commits.</summary>
    Task<Option<Article>> FindAsync(ArticleId id, CancellationToken cancellationToken);

    /// <summary>The article in either state as it is stored, for a query: nothing that changes it is saved.</summary>
    Task<Option<Article>> ReadAsync(ArticleId id, CancellationToken cancellationToken);

    /// <summary>One page of every article, drafts included, in the order asked for, projected in the database.</summary>
    Task<PagedResult<ArticleSummaryDto>> ListAsync(Sorting<ArticleSortField> sorting, Page page, PageSize pageSize, CancellationToken cancellationToken);

    /// <summary>One page of the published articles whose title or text contains the term (all of them for an empty term).</summary>
    Task<PagedResult<ArticleSummaryDto>> SearchPublishedAsync(string search, Sorting<ArticleSortField> sorting, Page page, PageSize pageSize, CancellationToken cancellationToken);
}
