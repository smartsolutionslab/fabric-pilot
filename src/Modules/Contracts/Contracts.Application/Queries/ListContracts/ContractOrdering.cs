using System.Diagnostics;
using System.Linq.Expressions;
using FabricPilot.Modules.Contracts.Domain.Contracts;

namespace FabricPilot.Modules.Contracts.Application.Queries.ListContracts;

public static class ContractOrdering
{
    /// <summary>Sorted as asked, then by id in the same direction, so contracts with the same key keep one order across pages.</summary>
    public static IOrderedQueryable<Contract> SortedBy(this IQueryable<Contract> contracts, Sorting<ContractSortField> sorting)
    {
        Ensure.That(contracts).IsNotNull();
        Ensure.That(sorting).IsNotNull();

        return sorting.Field switch
        {
            ContractSortField.EndsOn => By(contracts, contract => contract.EndsOn, sorting.Direction),
            ContractSortField.StartsOn => By(contracts, contract => contract.StartsOn, sorting.Direction),
            ContractSortField.Product => By(contracts, contract => contract.Product, sorting.Direction),
            _ => throw new UnreachableException($"The list query refuses the sort field {sorting.Field}."),
        };
    }

    private static IOrderedQueryable<Contract> By<TKey>(IQueryable<Contract> contracts, Expression<Func<Contract, TKey>> key, SortDirection direction) =>
        direction == SortDirection.Ascending
            ? contracts.OrderBy(key).ThenBy(contract => contract.Id)
            : contracts.OrderByDescending(key).ThenByDescending(contract => contract.Id);
}
