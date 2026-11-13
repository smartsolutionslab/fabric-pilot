using FabricPilot.Modules.Knowledge.Domain.Articles;

namespace FabricPilot.Modules.Knowledge.Application;

/// <summary>
/// The passages nearest a question, taken only from articles as they stand now. The index is written after the
/// article, so it can hold text the article no longer says, for good when indexing failed, and a search can find a
/// chunk just before its article is changed: a chunk is a passage only while its article is published, indexed, and
/// still holds that very text.
/// </summary>
public static class CurrentPassages
{
    /// <param name="search">The index's nearest chunks, at most the count it is given, nearest first.</param>
    /// <param name="articles">The articles with the given ids, as the articles table holds them now.</param>
    /// <param name="count">How many passages to find; when chunks are left out, the search asks for twice as many,
    /// until it has them or the index has no more.</param>
    public static async Task<IReadOnlyList<ArticlePassage>> NearestAsync(
        Func<int, CancellationToken, Task<IReadOnlyList<IndexedChunk>>> search,
        Func<IReadOnlyCollection<ArticleId>, CancellationToken, Task<IReadOnlyList<Article>>> articles,
        int count,
        CancellationToken cancellationToken)
    {
        Ensure.That(search).IsNotNull();
        Ensure.That(articles).IsNotNull();
        Ensure.That(count).AtLeast(1);

        for (var asked = count; ; asked *= 2)
        {
            var found = await search(asked, cancellationToken);
            var passages = await CurrentAsync(found, articles, cancellationToken);
            if (passages.Count >= count || found.Count < asked)
            {
                return [.. passages.Take(count)];
            }
        }
    }

    private static async Task<IReadOnlyList<ArticlePassage>> CurrentAsync(
        IReadOnlyList<IndexedChunk> found,
        Func<IReadOnlyCollection<ArticleId>, CancellationToken, Task<IReadOnlyList<Article>>> articles,
        CancellationToken cancellationToken)
    {
        List<(ArticleId Article, string Text)> chunks = [.. found
            .Select(chunk => (Article: ArticleChunks.ArticleOf(chunk.Key), chunk.Text))
            .Where(chunk => chunk.Article.HasValue)
            .Select(chunk => (chunk.Article.Value, chunk.Text))];
        HashSet<ArticleId> ids = [.. chunks.Select(chunk => chunk.Article)];
        var current = (await articles(ids, cancellationToken))
            .Where(article => article.State == ArticleState.Published && !article.NeedsIndexing)
            .ToDictionary(article => article.Id, article => (article.Title, Chunks: ArticleChunks.Of(article.Title, article.Body)));

        return [.. chunks
            .Where(chunk => current.TryGetValue(chunk.Article, out var article) && article.Chunks.Contains(chunk.Text))
            .Select(chunk => new ArticlePassage(chunk.Article, current[chunk.Article].Title, chunk.Text))];
    }
}
