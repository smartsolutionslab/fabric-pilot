using FabricPilot.BuildingBlocks.Rag;
using FabricPilot.Modules.Knowledge.Application;
using FabricPilot.Modules.Knowledge.Domain.Articles;
using FabricPilot.Modules.Knowledge.Infrastructure.Persistence;

namespace FabricPilot.Modules.Knowledge.Infrastructure;

/// <summary>
/// The articles' chunks in the module's <c>shared_embeddings</c> table, which every tenant, staff and work without a
/// tenant search; the index refuses a write outside <c>WritesSharedRecords()</c>.
/// </summary>
public sealed class SharedArticleIndex(SharedVectorIndex<KnowledgeDbContext> index) : IArticleIndex
{
    public async Task ReplaceAsync(ArticleId article, IReadOnlyList<string> chunks, int previousChunkCount, CancellationToken cancellationToken)
    {
        Ensure.That(article).IsNotNull();
        Ensure.That(chunks).IsNotNull();

        foreach (var (position, chunk) in chunks.Index())
        {
            await index.IndexAsync(ArticleChunks.Key(article, position), chunk, cancellationToken);
        }

        for (var position = chunks.Count; position < previousChunkCount; position++)
        {
            await index.RemoveAsync(ArticleChunks.Key(article, position), cancellationToken);
        }
    }
}
