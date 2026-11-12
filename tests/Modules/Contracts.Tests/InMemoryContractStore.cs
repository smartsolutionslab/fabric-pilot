using FabricPilot.Modules.Contracts.Application;
using FabricPilot.Modules.Contracts.Application.DTOs;
using FabricPilot.Modules.Contracts.Application.Queries.ListContracts;
using FabricPilot.Modules.Contracts.Domain.Contracts;

namespace FabricPilot.Modules.Contracts.Tests;

/// <summary>The contracts table of one tenant, in memory.</summary>
internal sealed class InMemoryContractStore : IContractStore
{
    public List<Contract> Saved { get; } = [];

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
}
