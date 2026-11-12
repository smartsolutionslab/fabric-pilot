using FabricPilot.Modules.Contracts.Application;
using FabricPilot.Modules.Contracts.Application.DTOs;
using FabricPilot.Modules.Contracts.Application.Queries.ListContracts;
using FabricPilot.Modules.Contracts.Domain.Contracts;
using FabricPilot.Modules.Contracts.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FabricPilot.Modules.Contracts.Infrastructure;

/// <summary>Every read is filtered to the current tenant by the DbContext's query filter.</summary>
public sealed class ContractStore(ContractsDbContext context) : IContractStore
{
    public async Task<PagedResult<ContractSummaryDto>> ListAsync(Sorting<ContractSortField> sorting, Page page, PageSize pageSize, CancellationToken cancellationToken)
    {
        Ensure.That(sorting).IsNotNull();
        Ensure.That(page).IsNotNull();
        Ensure.That(pageSize).IsNotNull();

        var contracts = context.Set<Contract>().AsNoTracking();
        var total = await contracts.CountAsync(cancellationToken);
        var items = await contracts
            .SortedBy(sorting)
            .Skip(page.Skip(pageSize))
            .Take(pageSize.Value)
            .Select(ContractMappings.ToContractSummaryDtoExpression())
            .ToListAsync(cancellationToken);
        return new PagedResult<ContractSummaryDto>(items, total, page.Value, pageSize.Value);
    }

    /// <summary>Inside <c>AcrossTenants()</c> the query filter lets every tenant's rows through.</summary>
    public async Task<PagedResult<ContractSummaryDto>> ListUpForRenewalAsync(DateOnly today, Page page, PageSize pageSize, CancellationToken cancellationToken)
    {
        Ensure.That(page).IsNotNull();
        Ensure.That(pageSize).IsNotNull();

        var contracts = context.Set<Contract>().AsNoTracking().Where(Contract.UpForRenewal(today));
        var total = await contracts.CountAsync(cancellationToken);
        var items = await contracts
            .OrderBy(contract => contract.RenewsOn)
            .ThenBy(contract => contract.Id)
            .Skip(page.Skip(pageSize))
            .Take(pageSize.Value)
            .Select(ContractMappings.ToContractSummaryDtoExpression())
            .ToListAsync(cancellationToken);
        return new PagedResult<ContractSummaryDto>(items, total, page.Value, pageSize.Value);
    }
}
