using FabricPilot.Tests.Fakes;
using Microsoft.Extensions.AI.Evaluation.Quality;

namespace FabricPilot.Evals.Tests;

/// <summary>
/// The gate on the suggested answers: every ticket of the dataset is answered through the module's handler and scored,
/// also for whether the draft cites the sources a good answer cites. A model that follows the instruction passes; the
/// degraded prompt fails the gate even with a judge that gives every answer full marks. No test calls a model.
/// </summary>
public sealed class SuggestAnswerGateTests : IDisposable
{
    private const double Threshold = 4;

    private readonly DirectoryInfo results = Directory.CreateTempSubdirectory("evals-");

    [Theory]
    [InlineData(new[] { "A" }, new[] { "A" }, 5)]
    [InlineData(new string[0], new string[0], 5)]
    [InlineData(new[] { "A", "B" }, new[] { "B", "A" }, 5)]
    [InlineData(new[] { "A" }, new string[0], 1)]
    [InlineData(new string[0], new[] { "A" }, 1)]
    [InlineData(new[] { "A" }, new[] { "B" }, 1)]
    [InlineData(new[] { "A", "B" }, new[] { "A" }, 3)]
    [InlineData(new[] { "A" }, new[] { "A", "B" }, 3)]
    public void The_sources_score_runs_from_1_for_no_expected_source_cited_to_5_for_exactly_the_expected_ones(string[] expected, string[] cited, double score)
    {
        SourcesEvaluator.Score(expected, cited).ShouldBe(score);
    }

    [Fact]
    public async Task Every_ticket_of_the_dataset_is_scored_for_its_sources()
    {
        var scores = await RunAsync(EvalVariant.Normal, judgeScore: 5);

        var tickets = SupportTicketDataset.Load().Count;
        scores.All.Count(score => score.UseCase == "suggest-answer" && score.Metric == SourcesEvaluator.SourcesMetricName).ShouldBe(tickets);
        scores.All.Count(score => score.UseCase == "suggest-answer" && score.Metric == GroundednessEvaluator.GroundednessMetricName).ShouldBe(tickets);
        scores.All.ShouldNotContain(score => score.UseCase == "ask-the-model" && score.Metric == SourcesEvaluator.SourcesMetricName);
    }

    [Fact]
    public async Task A_model_that_cites_the_expected_sources_passes_the_gate()
    {
        var scores = await RunAsync(EvalVariant.Normal, judgeScore: 5);

        scores.BelowThreshold(Threshold).ShouldBeEmpty();
        scores.All.Where(score => score.Metric == SourcesEvaluator.SourcesMetricName).ShouldAllBe(score => score.Value == 5);
    }

    [Fact]
    public async Task The_degraded_prompt_fails_the_gate_on_the_suggested_answers_sources_even_when_the_judge_gives_full_marks()
    {
        var scores = await RunAsync(EvalVariant.Degraded, judgeScore: 5);

        var violations = scores.BelowThreshold(Threshold);

        violations.ShouldContain(violation => violation.StartsWith($"suggest-answer: {SourcesEvaluator.SourcesMetricName} has a mean of ", StringComparison.Ordinal)
            && violation.Contains("below the threshold of 4", StringComparison.Ordinal));
        violations.ShouldNotContain(violation => violation.StartsWith("ask-the-model", StringComparison.Ordinal));
    }

    [Fact]
    public async Task A_model_whose_answer_cannot_be_read_fails_the_gate_on_sources()
    {
        using var model = new FakeChatClient("Not the asked shape.");
        using var judge = new FakeJudge(5);

        var scores = await EvalRun.RunAsync(UseCases.All, model, judge, results.FullName, "gate", TestContext.Current.CancellationToken);

        scores.BelowThreshold(Threshold).ShouldContain(violation => violation.StartsWith($"suggest-answer: {SourcesEvaluator.SourcesMetricName} has a mean of ", StringComparison.Ordinal));
    }

    public void Dispose() => results.Delete(recursive: true);

    private async Task<EvalScores> RunAsync(EvalVariant variant, int judgeScore)
    {
        using var model = new InstructionFollowingModel(ExpectedSources());
        using var judge = new FakeJudge(judgeScore);
        return await EvalRun.RunAsync(UseCases.All, variant.Answering(model), judge, results.FullName, "gate", TestContext.Current.CancellationToken);
    }

    private static Dictionary<string, IReadOnlyList<string>> ExpectedSources() =>
        UseCases.All
            .SelectMany(useCase => useCase.Examples)
            .Where(example => example.ExpectedSources is not null)
            .ToDictionary(example => example.Input, example => example.ExpectedSources!);
}
