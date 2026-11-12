using FabricPilot.Modules.Knowledge.Domain.Articles;

namespace FabricPilot.Modules.Knowledge.Application;

public static class ArticleSearch
{
    /// <summary>
    /// The published articles whose title or text contains the term, compared without case; every published one for an
    /// empty term. Drafts never: a customer sees only what staff published.
    /// </summary>
    public static IQueryable<Article> PublishedMatching(this IQueryable<Article> articles, string search)
    {
        Ensure.That(articles).IsNotNull();
        Ensure.That(search).IsNotNull();

        var published = articles.Where(article => article.State == ArticleState.Published);
        var term = search.Trim();
        return term.Length == 0
            ? published
            : published.Where(article => article.Title.Contains(term, StringComparison.OrdinalIgnoreCase) || article.Body.Contains(term, StringComparison.OrdinalIgnoreCase));
    }
}
