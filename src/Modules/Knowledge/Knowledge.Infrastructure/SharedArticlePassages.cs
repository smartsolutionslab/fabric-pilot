using FabricPilot.BuildingBlocks.Rag;
using FabricPilot.Modules.Knowledge.Application;
using FabricPilot.Modules.Knowledge.Domain.Articles;
using FabricPilot.Modules.Knowledge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FabricPilot.Modules.Knowledge.Infrastructure;

/// <summary>
/// The nearest chunks in the module's <c>shared_embeddings</c> table, each with its article's title as the articles table
/// holds it now. A chunk whose key names no article, an article that is not published (any more), or one whose current
/// text is not indexed yet, is left out: the index is written after the article, so it can hold text the article no
/// longer says, for good when indexing failed.
/// </summary>
public sealed class SharedArticlePassages(SharedVectorIndex<KnowledgeDbContext> index, KnowledgeDbContext context) : IArticlePassages
{
    public async Task<IReadOnlyList<ArticlePassage>> NearestAsync(string question, int count, CancellationToken cancellationToken)
    {
        var found = await index.SearchAsync(question, count, cancellationToken);
        List<(ArticleId Article, string Text)> chunks = [.. found
            .Select(passage => (Article: ArticleChunks.ArticleOf(passage.Key), passage.Text))
            .Where(chunk => chunk.Article.HasValue)
            .Select(chunk => (chunk.Article.Value, chunk.Text))];
        List<ArticleId> ids = [.. chunks.Select(chunk => chunk.Article).Distinct()];
        var titles = await context.Set<Article>().AsNoTracking().Published()
            .Where(article => !article.NeedsIndexing && ids.Contains(article.Id))
            .ToDictionaryAsync(article => article.Id, article => article.Title, cancellationToken);

        return [.. chunks
            .Where(chunk => titles.ContainsKey(chunk.Article))
            .Select(chunk => new ArticlePassage(chunk.Article, titles[chunk.Article], chunk.Text))];
    }
}
