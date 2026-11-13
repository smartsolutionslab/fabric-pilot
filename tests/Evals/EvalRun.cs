using System.Globalization;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.Quality;
using Microsoft.Extensions.AI.Evaluation.Reporting;
using Microsoft.Extensions.AI.Evaluation.Reporting.Storage;

namespace FabricPilot.Evals;

/// <summary>
/// Runs the use cases: the model answers every example, and every answer is scored for relevance, for groundedness
/// where the example supplies context, by the project's own checks, and for its sources where the example expects some. The judge scores the judged metrics. Every
/// result is written to the results directory under the execution's name, for <c>dotnet aieval report</c>.
/// </summary>
public static class EvalRun
{
    public static async Task<EvalScores> RunAsync(
        IReadOnlyList<EvalUseCase> useCases,
        IChatClient model,
        IChatClient judge,
        string resultsDirectory,
        string executionName,
        CancellationToken cancellationToken)
    {
        Ensure.That(useCases).IsNotNull();
        Ensure.That(model).IsNotNull();
        Ensure.That(judge).IsNotNull();

        if (useCases.Count == 0)
        {
            throw new InvalidOperationException("There are no use cases to evaluate: list them in UseCases.All, each with examples.");
        }

        var withoutExamples = useCases.Where(useCase => useCase.Examples.Count == 0).Select(useCase => $"'{useCase.Name}'").ToList();
        if (withoutExamples.Count > 0)
        {
            throw new InvalidOperationException($"These use cases have no examples, so nothing would hold them to the threshold: {string.Join(", ", withoutExamples)}. Give each examples that cover what it is for.");
        }

        // No sampling options: the evaluators ask for Temperature 0 and TopP 1, which some models refuse; every provider's
        // defaults apply instead.
        var judgeConfiguration = new ChatConfiguration(judge.AsBuilder().ConfigureOptions(WithoutSamplingOptions).Build());
        Dictionary<(bool Grounded, bool Sourced), ReportingConfiguration> reporting = [];
        ReportingConfiguration For(bool grounded, bool sourced)
        {
            if (!reporting.TryGetValue((grounded, sourced), out var configuration))
            {
                List<IEvaluator> evaluators = [new RelevanceEvaluator()];
                if (grounded)
                {
                    evaluators.Add(new GroundednessEvaluator());
                }

                evaluators.Add(new AnsweredEvaluator());
                if (sourced)
                {
                    evaluators.Add(new SourcesEvaluator());
                }

                configuration = Reporting(resultsDirectory, executionName, judgeConfiguration, [.. evaluators]);
                reporting[(grounded, sourced)] = configuration;
            }

            return configuration;
        }

        List<EvalScore> scores = [];
        foreach (var useCase in useCases)
        {
            for (var index = 0; index < useCase.Examples.Count; index++)
            {
                var current = useCase.Examples[index];
                var (input, groundingContext, expectedSources) = current;
                var example = (index + 1).ToString(CultureInfo.InvariantCulture);
                List<EvaluationContext> context = [];
                if (groundingContext is not null)
                {
                    context.Add(new GroundednessEvaluatorContext(groundingContext));
                }

                if (expectedSources is not null)
                {
                    context.Add(new ExpectedSourcesContext(expectedSources));
                }

                await using var scenario = await For(groundingContext is not null, expectedSources is not null)
                    .CreateScenarioRunAsync(useCase.Name, example, cancellationToken: cancellationToken);
                var response = await useCase.Answer(model, current, cancellationToken);
                var result = await scenario.EvaluateAsync([new ChatMessage(ChatRole.User, input)], response, context, cancellationToken);

                foreach (var metric in result.Metrics.Values)
                {
                    var value = (metric as NumericMetric)?.Value;
                    scores.Add(new EvalScore(useCase.Name, example, metric.Name, value));
                    if (metric is NumericMetric { Value: { } number } numeric && !double.IsFinite(number))
                    {
                        // The results are JSON, which has no NaN or infinity: keep the score in the run, not in the file.
                        numeric.Value = null;
                        numeric.AddDiagnostics(EvaluationDiagnostic.Error($"The score {number.ToString(CultureInfo.InvariantCulture)} is not a finite number."));
                    }
                }
            }
        }

        return new EvalScores(scores);
    }

    private static void WithoutSamplingOptions(ChatOptions options)
    {
        options.Temperature = null;
        options.TopP = null;
    }

    private static ReportingConfiguration Reporting(string resultsDirectory, string executionName, ChatConfiguration judge, params IEvaluator[] evaluators) =>
        DiskBasedReportingConfiguration.Create(resultsDirectory, evaluators, judge, enableResponseCaching: false, executionName: executionName);
}
