using FabricPilot.BuildingBlocks.Messaging;

namespace FabricPilot.Agents.Tests;

public sealed class ListWordsHandler : IQueryHandler<ListWords, Result<PagedResult<string>, ApiError>>
{
    public Task<Result<PagedResult<string>, ApiError>> HandleAsync(ListWords query, CancellationToken cancellationToken)
    {
        Ensure.That(query).IsNotNull();

        var words = query.Text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        List<string> page = [.. words.Skip(query.Page.Skip(query.PageSize)).Take(query.PageSize.Value)];
        var result = new PagedResult<string>(page, words.Length, query.Page.Value, query.PageSize.Value);
        return Task.FromResult<Result<PagedResult<string>, ApiError>>(Success(result));
    }
}
