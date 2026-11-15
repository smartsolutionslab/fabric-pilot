using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Contracts.Application.DTOs;

namespace FabricPilot.Modules.Contracts.Application.Queries.ListContractsUpForRenewal;

/// <summary>Today is the UTC date of the clock; the window around it is the domain's.</summary>
public sealed class ListContractsUpForRenewalHandler(IContractStore contracts, TimeProvider time)
    : IQueryHandler<ListContractsUpForRenewal, Result<PagedResult<ContractSummaryDto>, ApiError>>
{
    public async Task<Result<PagedResult<ContractSummaryDto>, ApiError>> HandleAsync(ListContractsUpForRenewal query, CancellationToken cancellationToken)
    {
        Ensure.That(query).IsNotNull();

        var (page, pageSize) = query;
        var today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);
        return Success(await contracts.ListUpForRenewalAsync(today, page, pageSize, cancellationToken));
    }
}
