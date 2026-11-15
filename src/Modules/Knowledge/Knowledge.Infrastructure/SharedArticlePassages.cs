using FabricPilot.BuildingBlocks.Rag;
using FabricPilot.Modules.Knowledge.Application;
using FabricPilot.Modules.Knowledge.Domain.Articles;
using FabricPilot.Modules.Knowledge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FabricPilot.Modules.Knowledge.Infrastructure;

/// <summary>
/// The nearest chunks in the module's <c>shared_embeddings</c> table that the articles, as the table holds them now,
/// still say (<see cref="CurrentPassages"/>).
/// </summary>
public sealed class SharedArticlePassages(SharedVectorIndex<KnowledgeDbContext> index, KnowledgeDbContext context) : IArticlePassages
{
    public Task<IReadOnlyList<ArticlePassage>> NearestAsync(string question, int count, CancellationToken cancellationToken) =>
        CurrentPassages.NearestAsync(
            async (asked, cancellation) => [.. (await index.SearchAsync(question, asked, cancellation)).Select(passage => new IndexedChunk(passage.Key, passage.Text))],
            async (ids, cancellation) => await context.Set<Article>().AsNoTracking().Where(article => ids.Contains(article.Id)).ToListAsync(cancellation),
            count,
            cancellationToken);
}
