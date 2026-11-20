namespace FabricPilot.Evals;

/// <summary>
/// One input of a use case. <paramref name="GroundingContext"/> is what the answer must stay true to, the records a
/// <c>rag</c> use case retrieves, say; with it the example is also judged for groundedness. <paramref name="ExpectedSources"/>
/// are the records a good answer cites, by title, none when no record answers it; with them the example is also scored
/// by <see cref="SourcesEvaluator"/>. Two examples with the same values are equal, their expected sources compared item
/// by item.
/// </summary>
public sealed record EvalExample(string Input, string? GroundingContext = null, IReadOnlyList<string>? ExpectedSources = null)
{
    public bool Equals(EvalExample? other) =>
        other is not null
        && Input == other.Input
        && GroundingContext == other.GroundingContext
        && (ExpectedSources is null ? other.ExpectedSources is null : other.ExpectedSources is not null && ExpectedSources.SequenceEqual(other.ExpectedSources));

    public override int GetHashCode() => HashCode.Combine(Input, GroundingContext, ExpectedSources?.Count);
}
