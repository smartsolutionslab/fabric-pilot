using FabricPilot.Modules.Knowledge.Domain.Articles;

namespace FabricPilot.Modules.Knowledge.Application;

public static class ArticleIndexing
{
    /// <summary>
    /// Writes the article's chunks as it now stands and removes every other chunk key it could have, whatever an earlier
    /// edit or a failed write left there. Run it after the article is committed, under its lock: the index then never
    /// holds text that is not published, and a failure here leaves a published article to be indexed again by its next edit.
    /// </summary>
    public static async Task ReplaceAsync(this IArticleIndex index, Article article, CancellationToken cancellationToken)
    {
        Ensure.That(index).IsNotNull();
        Ensure.That(article).IsNotNull();

        var chunks = ArticleChunks.Of(article.Title, article.Body);
        foreach (var (position, chunk) in chunks.Index())
        {
            await index.IndexAsync(ArticleChunks.Key(article.Id, position), chunk, cancellationToken);
        }

        for (var position = chunks.Count; position < ArticleChunks.MaximumCount; position++)
        {
            await index.RemoveAsync(ArticleChunks.Key(article.Id, position), cancellationToken);
        }
    }
}
