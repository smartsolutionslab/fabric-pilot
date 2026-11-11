using FabricPilot.BuildingBlocks.Messaging;

namespace FabricPilot.Agents.Tests;

public sealed class LongestWordHandler : IQueryHandler<LongestWord, Result<Option<string>, ApiError>>
{
    public Task<Result<Option<string>, ApiError>> HandleAsync(LongestWord query, CancellationToken cancellationToken)
    {
        Ensure.That(query).IsNotNull();

        var atLeast = query.AtLeast.GetOrDefault(0);
        var longest = query.Text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Where(word => word.Length >= atLeast).MaxBy(word => word.Length);
        var found = longest is null ? Option<string>.None : Option<string>.Some(longest);
        return Task.FromResult<Result<Option<string>, ApiError>>(Success(found));
    }
}
