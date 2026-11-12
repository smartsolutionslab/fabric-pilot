using FabricPilot.Modules.Contracts.Application;
using FabricPilot.Modules.Contracts.Application.DTOs;
using FabricPilot.Modules.Contracts.Application.Queries.ListContracts;
using FabricPilot.Modules.Contracts.Domain.Contracts;

namespace FabricPilot.Modules.Contracts.Tests;

/// <summary>The contracts table of one tenant, in memory.</summary>
internal sealed class InMemoryContractStore : IContractStore
{
    public List<Contract> Saved { get; } = [];

    /// <summary>The day the last renewal list was asked for.</summary>
    public DateOnly? AskedForRenewalsOn { get; private set; }

    public Task<PagedResult<ContractSummaryDto>> ListAsync(Sorting<ContractSortField> sorting, Page page, PageSize pageSize, CancellationToken cancellationToken)
    {
        Ensure.That(sorting).IsNotNull();
        Ensure.That(page).IsNotNull();
        Ensure.That(pageSize).IsNotNull();

        var items = Saved.AsQueryable()
            .SortedBy(sorting)
            .Skip(page.Skip(pageSize))
            .Take(pageSize.Value)
            .Select(ContractMappings.ToContractSummaryDtoExpression());
        return Task.FromResult(new PagedResult<ContractSummaryDto>([.. items], Saved.Count, page.Value, pageSize.Value));
    }

    public Task<PagedResult<ContractSummaryDto>> ListUpForRenewalAsync(DateOnly today, Page page, PageSize pageSize, CancellationToken cancellationToken)
    {
        Ensure.That(page).IsNotNull();
        Ensure.That(pageSize).IsNotNull();

        AskedForRenewalsOn = today;
        var upForRenewal = Saved.AsQueryable().Where(Contract.UpForRenewal(today));
        var items = upForRenewal
            .OrderBy(contract => contract.RenewsOn)
            .ThenBy(contract => contract.Id.Value)
            .Skip(page.Skip(pageSize))
            .Take(pageSize.Value)
            .Select(ContractMappings.ToContractSummaryDtoExpression());
        return Task.FromResult(new PagedResult<ContractSummaryDto>([.. items], upForRenewal.Count(), page.Value, pageSize.Value));
    }
}
