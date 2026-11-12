using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Contracts.Application.DTOs;

namespace FabricPilot.Modules.Contracts.Application.Queries.ListContracts;

public sealed class ListContractsHandler(IContractStore contracts) : IQueryHandler<ListContracts, Result<PagedResult<ContractSummaryDto>, ApiError>>
{
    public async Task<Result<PagedResult<ContractSummaryDto>, ApiError>> HandleAsync(ListContracts query, CancellationToken cancellationToken)
    {
        Ensure.That(query).IsNotNull();

        var (page, pageSize, sorting) = query;
        Ensure.That(sorting).IsNotNull();
        Ensure.That((int)sorting.Field, nameof(sorting)).Satisfies(field => Enum.IsDefined((ContractSortField)field), $"The contract list cannot be sorted by {sorting.Field}.");
        Ensure.That((int)sorting.Direction, nameof(sorting)).Satisfies(direction => Enum.IsDefined((SortDirection)direction), $"{sorting.Direction} is not a sort direction.");

        return Success(await contracts.ListAsync(sorting, page, pageSize, cancellationToken));
    }
}
