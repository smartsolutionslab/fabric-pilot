using FabricPilot.Modules.Knowledge.Application;
using FabricPilot.Modules.Knowledge.Domain.Articles;

namespace FabricPilot.Modules.Knowledge.Tests;

/// <summary>Per-article locks in memory: how many are held now, which articles were locked, and which another change holds.</summary>
internal sealed class InMemoryArticleLocks : IArticleLocks
{
    public int Held { get; private set; }

    public List<ArticleId> Locked { get; } = [];

    /// <summary>Articles whose lock another change holds: taking it fails at once.</summary>
    public HashSet<ArticleId> HeldElsewhere { get; } = [];

    public Task<Option<IAsyncDisposable>> TryAcquireAsync(ArticleId article, CancellationToken cancellationToken)
    {
        if (HeldElsewhere.Contains(article))
        {
            return Task.FromResult(Option<IAsyncDisposable>.None);
        }

        Held++;
        Locked.Add(article);
        return Task.FromResult(Option<IAsyncDisposable>.Some(new Release(this)));
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
