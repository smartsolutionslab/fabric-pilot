using FabricPilot.BuildingBlocks.Messaging;

namespace FabricPilot.Agents.Tests;

public sealed class BrowseWordsHandler : IQueryHandler<BrowseWords, Result<PagedResult<string>, ApiError>>
{
    public Task<Result<PagedResult<string>, ApiError>> HandleAsync(BrowseWords query, CancellationToken cancellationToken)
    {
        Ensure.That(query).IsNotNull();

        var page = query.Page ?? Page.First;
        var words = query.Text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        List<string> items = [.. words.Skip(page.Skip(query.PageSize)).Take(query.PageSize.Value)];
        var result = new PagedResult<string>(items, words.Length, page.Value, query.PageSize.Value);
        return Task.FromResult<Result<PagedResult<string>, ApiError>>(Success(result));
    }
}
