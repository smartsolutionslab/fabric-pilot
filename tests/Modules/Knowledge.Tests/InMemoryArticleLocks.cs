using FabricPilot.Modules.Knowledge.Application;
using FabricPilot.Modules.Knowledge.Domain.Articles;

namespace FabricPilot.Modules.Knowledge.Tests;

/// <summary>Per-article locks in memory: how many are held now, and which articles were locked.</summary>
internal sealed class InMemoryArticleLocks : IArticleLocks
{
    public int Held { get; private set; }

    public List<ArticleId> Locked { get; } = [];

    public Task<IAsyncDisposable> AcquireAsync(ArticleId article, CancellationToken cancellationToken)
    {
        Held++;
        Locked.Add(article);
        return Task.FromResult<IAsyncDisposable>(new Release(this));
    }

    private sealed class Release(InMemoryArticleLocks locks) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            locks.Held--;
            return ValueTask.CompletedTask;
        }
    }
}
