using FabricPilot.Modules.Knowledge.Application;

namespace FabricPilot.Modules.Knowledge.Tests;

/// <summary>The shared index's nearest passages, in memory: the ones a test put there, nearest first, and every search asked.</summary>
internal sealed class InMemoryArticlePassages : IArticlePassages
{
    public List<ArticlePassage> Nearest { get; } = [];

    public List<(string Question, int Count)> Searches { get; } = [];

    public Task<IReadOnlyList<ArticlePassage>> NearestAsync(string question, int count, CancellationToken cancellationToken)
    {
        Searches.Add((question, count));
        return Task.FromResult<IReadOnlyList<ArticlePassage>>([.. Nearest.Take(count)]);
    }
}
