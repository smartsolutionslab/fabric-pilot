using FabricPilot.Modules.Knowledge.Application;
using FabricPilot.Modules.Knowledge.Domain.Articles;

namespace FabricPilot.Evals;

/// <summary>
/// The knowledge base for one example: its grounding context as the published articles the search finds, nearest first in
/// the order they are written, separated by <see cref="ArticleSeparator"/>. An article's first line is its title, as a
/// chunk is led by its article's title. No example's grounding context, no article.
/// </summary>
public sealed class EvalPassages(EvalExample example) : IArticlePassages
{
    /// <summary>What separates two articles in a grounding context; no article may hold it.</summary>
    public const string ArticleSeparator = "\n\n---\n\n";

    public Task<IReadOnlyList<ArticlePassage>> NearestAsync(string question, int count, CancellationToken cancellationToken)
    {
        Ensure.That(example).IsNotNull();

        if (example.GroundingContext is not { } articles)
        {
            return Task.FromResult<IReadOnlyList<ArticlePassage>>([]);
        }

        IReadOnlyList<ArticlePassage> passages = [.. articles
            .Split(ArticleSeparator)
            .Take(count)
            .Select(article => new ArticlePassage(ArticleId.New(), article.Split('\n', 2)[0].Trim(), article))];
        return Task.FromResult(passages);
    }
}
