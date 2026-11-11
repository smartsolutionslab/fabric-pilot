using FabricPilot.BuildingBlocks.Messaging;

namespace FabricPilot.Agents.Tests;

public sealed class CountWordsHandler : IQueryHandler<CountWords, Result<int, ApiError>>
{
    public Task<Result<int, ApiError>> HandleAsync(CountWords query, CancellationToken cancellationToken)
    {
        Ensure.That(query).IsNotNull();

        var words = query.Text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        if (words == 0)
        {
            return Task.FromResult<Result<int, ApiError>>(Failure<ApiError>(new NothingToCount()));
        }

        return Task.FromResult<Result<int, ApiError>>(Success(words));
    }
}
