using FabricPilot.Modules.Knowledge.Domain.Articles;

namespace FabricPilot.Modules.Knowledge.Application;

public static class ArticleSearch
{
    /// <summary>The published articles only: a customer never sees a draft. A store matches the search term within them.</summary>
    public static IQueryable<Article> Published(this IQueryable<Article> articles)
    {
        Ensure.That(articles).IsNotNull();

        return articles.Where(article => article.State == ArticleState.Published);
    }
}
