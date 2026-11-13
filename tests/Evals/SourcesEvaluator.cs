using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;

namespace FabricPilot.Evals;

/// <summary>
/// The project's own check on what an answer cites: the titles of the records it is drafted from, as the answer carries
/// them (<see cref="SuggestedAnswers.SourcesProperty"/>), against the example's expected sources. It needs no judge. A
/// draft where no record answers, or no draft where one does, scores 1; see <see cref="Score"/>.
/// </summary>
public sealed class SourcesEvaluator : IEvaluator
{
    public const string SourcesMetricName = "Sources";

    public IReadOnlyCollection<string> EvaluationMetricNames { get; } = [SourcesMetricName];

    /// <summary>
    /// 1 plus 4 times the share of the expected and the cited sources that are both (their Jaccard index): 5 for exactly
    /// the expected ones, in any order, and for none when none are expected; 1 when no expected source is cited.
    /// </summary>
    public static double Score(IReadOnlyCollection<string> expected, IReadOnlyCollection<string> cited)
    {
        Ensure.That(expected).IsNotNull();
        Ensure.That(cited).IsNotNull();

        var union = expected.Union(cited, StringComparer.Ordinal).Count();
        if (union == 0)
        {
            return 5;
        }

        var both = expected.Intersect(cited, StringComparer.Ordinal).Count();
        return 1 + (4.0 * both / union);
    }

    public ValueTask<EvaluationResult> EvaluateAsync(
        IEnumerable<ChatMessage> messages,
        ChatResponse modelResponse,
        ChatConfiguration? chatConfiguration = null,
        IEnumerable<EvaluationContext>? additionalContext = null,
        CancellationToken cancellationToken = default)
    {
        Ensure.That(modelResponse).IsNotNull();

        if (additionalContext?.OfType<ExpectedSourcesContext>().FirstOrDefault() is not { } context)
        {
            var unscored = new NumericMetric(SourcesMetricName, value: null, reason: "The example names no expected sources.");
            unscored.AddDiagnostics(EvaluationDiagnostic.Error($"{nameof(SourcesEvaluator)} needs an {nameof(ExpectedSourcesContext)}."));
            return ValueTask.FromResult(new EvaluationResult(unscored));
        }

        var expected = context.ExpectedSources;
        var cited = SuggestedAnswers.SourcesOf(modelResponse);
        var score = Score(expected, cited);
        var reason = $"Expected {Listed(expected)}; cited {Listed(cited)}.";
        return ValueTask.FromResult(new EvaluationResult(new NumericMetric(SourcesMetricName, score, reason)));
    }

    private static string Listed(IReadOnlyCollection<string> titles) =>
        titles.Count == 0 ? "none" : string.Join(", ", titles.Select(title => $"'{title}'"));
}
