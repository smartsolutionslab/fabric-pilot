using FabricPilot.Modules.Knowledge.Application;

namespace FabricPilot.Modules.Knowledge.Tests;

/// <summary>The shared vector index, in memory: each chunk's text under its key, and how many writes ran without the article's lock.</summary>
internal sealed class InMemoryArticleIndex(InMemoryArticleLocks locks) : IArticleIndex
{
    public Dictionary<string, string> Chunks { get; } = [];

    public int WritesOutsideTheLock { get; private set; }

    /// <summary>Every write fails, as when the embeddings provider cannot be reached.</summary>
    public bool Unavailable { get; set; }

    public Task IndexAsync(string key, string text, CancellationToken cancellationToken)
    {
        Count();
        Chunks[key] = text;
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken)
    {
        Count();
        Chunks.Remove(key);
        return Task.CompletedTask;
    }

    private void Count()
    {
        if (Unavailable)
        {
            throw new HttpRequestException("The embeddings provider cannot be reached.");
        }

        if (locks.Held == 0)
        {
            WritesOutsideTheLock++;
        }
    }
}
