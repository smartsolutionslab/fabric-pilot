using FabricPilot.Modules.Contracts.Application.DTOs;
using FabricPilot.Modules.Contracts.Application.Queries.ListContracts;

namespace FabricPilot.Modules.Contracts.Application;

/// <summary>The contracts table, the current tenant's rows only; implemented in the module's Infrastructure project.</summary>
public interface IContractStore
{
    /// <summary>One page of contract summaries in the order asked for, projected in the database.</summary>
    Task<PagedResult<ContractSummaryDto>> ListAsync(Sorting<ContractSortField> sorting, Page page, PageSize pageSize, CancellationToken cancellationToken);

    /// <summary>One page of the contracts up for renewal on <paramref name="today"/>, the soonest to renew first.</summary>
    Task<PagedResult<ContractSummaryDto>> ListUpForRenewalAsync(DateOnly today, Page page, PageSize pageSize, CancellationToken cancellationToken);
}
