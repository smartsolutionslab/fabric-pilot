using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Knowledge.Application.DTOs;

namespace FabricPilot.Modules.Knowledge.Application.Queries.SearchArticles;

/// <summary>One page of the published articles whose title or text contains <see cref="Search"/>; every published one for an empty term.</summary>
public sealed record SearchArticles(string Search, Page Page, PageSize PageSize, Sorting<ArticleSortField> Sorting)
    : IQuery<Result<PagedResult<ArticleSummaryDto>, ApiError>>
{
    /// <summary>The order the list has when the caller asks for none.</summary>
    public static Sorting<ArticleSortField> LatestFirst { get; } = new(ArticleSortField.ChangedAt, SortDirection.Descending);
}
