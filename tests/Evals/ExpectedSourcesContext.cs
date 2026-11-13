using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;

namespace FabricPilot.Evals;

/// <summary>The titles of the records a good answer to the example cites, for <see cref="SourcesEvaluator"/>; none when no record answers it.</summary>
public sealed class ExpectedSourcesContext(IReadOnlyList<string> expectedSources)
    : EvaluationContext(ContextName, [.. expectedSources.Select(source => new TextContent(source))])
{
    public const string ContextName = "Expected sources";

    public IReadOnlyList<string> ExpectedSources { get; } = expectedSources;
}
