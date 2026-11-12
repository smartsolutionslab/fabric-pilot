using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;

namespace FabricPilot.Evals;

/// <summary>
/// A project's own check is an <see cref="IEvaluator"/> like this one: it scores every answer from 1 to 5 as a
/// <see cref="NumericMetric"/>, and the threshold applies to its mean as to the judged metrics'. This one needs no
/// judge: an empty answer scores 1.
/// </summary>
public sealed class AnsweredEvaluator : IEvaluator
{
    public const string AnsweredMetricName = "Answered";

    public IReadOnlyCollection<string> EvaluationMetricNames { get; } = [AnsweredMetricName];

    public ValueTask<EvaluationResult> EvaluateAsync(
        IEnumerable<ChatMessage> messages,
        ChatResponse modelResponse,
        ChatConfiguration? chatConfiguration = null,
        IEnumerable<EvaluationContext>? additionalContext = null,
        CancellationToken cancellationToken = default)
    {
        Ensure.That(modelResponse).IsNotNull();

        var answered = !string.IsNullOrWhiteSpace(modelResponse.Text);
        var metric = new NumericMetric(AnsweredMetricName, answered ? 5 : 1, answered ? "The model answered." : "The model's answer is empty.");
        return ValueTask.FromResult(new EvaluationResult(metric));
    }
}
