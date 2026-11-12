using FabricPilot.BuildingBlocks.Rag;
using FabricPilot.Modules.Knowledge.Application;
using FabricPilot.Modules.Knowledge.Infrastructure.Persistence;

namespace FabricPilot.Modules.Knowledge.Infrastructure;

/// <summary>
/// The articles' chunks in the module's <c>shared_embeddings</c> table, which every tenant, staff and work without a
/// tenant search; the index refuses a write outside <c>WritesSharedRecords()</c>.
/// </summary>
public sealed class SharedArticleIndex(SharedVectorIndex<KnowledgeDbContext> index) : IArticleIndex
{
    public Task IndexAsync(string key, string text, CancellationToken cancellationToken) => index.IndexAsync(key, text, cancellationToken);

    public Task RemoveAsync(string key, CancellationToken cancellationToken) => index.RemoveAsync(key, cancellationToken);
}
