using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Knowledge.Application.DTOs;

namespace FabricPilot.Modules.Knowledge.Application.Queries.ListArticles;

/// <summary>One page of every article, drafts included, for the staff who write them.</summary>
public sealed record ListArticles(Page Page, PageSize PageSize, Sorting<ArticleSortField> Sorting)
    : IQuery<Result<PagedResult<ArticleSummaryDto>, ApiError>>
{
    /// <summary>The order the list has when the caller asks for none.</summary>
    public static Sorting<ArticleSortField> LatestFirst { get; } = new(ArticleSortField.ChangedAt, SortDirection.Descending);
}
