using FabricPilot.Modules.Knowledge.Application;
using FabricPilot.Modules.Knowledge.Domain.Articles;

namespace FabricPilot.Modules.Knowledge.Tests;

/// <summary>The shared vector index, in memory: each chunk's text under its key.</summary>
internal sealed class InMemoryArticleIndex : IArticleIndex
{
    public Dictionary<string, string> Chunks { get; } = [];

    public Task ReplaceAsync(ArticleId article, IReadOnlyList<string> chunks, int previousChunkCount, CancellationToken cancellationToken)
    {
        Ensure.That(chunks).IsNotNull();

        foreach (var (position, chunk) in chunks.Index())
        {
            Chunks[ArticleChunks.Key(article, position)] = chunk;
        }

        for (var position = chunks.Count; position < previousChunkCount; position++)
        {
            Chunks.Remove(ArticleChunks.Key(article, position));
        }

        return Task.CompletedTask;
    }
}
