using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Contracts.Application.DTOs;

namespace FabricPilot.Modules.Contracts.Application.Queries.ListContracts;

/// <summary>One page of the caller's tenant's contracts, in the order asked for.</summary>
public sealed record ListContracts(Page Page, PageSize PageSize, Sorting<ContractSortField> Sorting)
    : IQuery<Result<PagedResult<ContractSummaryDto>, ApiError>>
{
    /// <summary>The order the list has when the caller asks for none: the next contract to end first.</summary>
    public static Sorting<ContractSortField> EndingFirst { get; } = new(ContractSortField.EndsOn, SortDirection.Ascending);
}
