using FabricPilot.Modules.Knowledge.Application;
using FabricPilot.Modules.Knowledge.Domain.Articles;

namespace FabricPilot.Evals;

/// <summary>
/// The knowledge base for one example: its grounding context as the one published article the search finds, its first
/// line the title, as a chunk is led by its article's title. No example's grounding context, no article.
/// </summary>
public sealed class EvalPassages(EvalExample example) : IArticlePassages
{
    public Task<IReadOnlyList<ArticlePassage>> NearestAsync(string question, int count, CancellationToken cancellationToken)
    {
        Ensure.That(example).IsNotNull();

        if (example.GroundingContext is not { } article)
        {
            return Task.FromResult<IReadOnlyList<ArticlePassage>>([]);
        }

        var title = article.Split('\n', 2)[0].Trim();
        return Task.FromResult<IReadOnlyList<ArticlePassage>>([new ArticlePassage(ArticleId.New(), title, article)]);
    }
}
