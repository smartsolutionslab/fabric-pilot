using FabricPilot.Modules.Contracts.Application.Queries.ListContracts;
using FabricPilot.Modules.Contracts.Domain.Contracts;
using SortDirection = FabricPilot.Kernel.SortDirection;

namespace FabricPilot.Modules.Contracts.Tests.Queries;

public class ContractOrderingTests
{
    [Theory]
    [InlineData(ContractSortField.EndsOn, SortDirection.Ascending, "Banana,Cherry,Apple")]
    [InlineData(ContractSortField.EndsOn, SortDirection.Descending, "Apple,Cherry,Banana")]
    [InlineData(ContractSortField.StartsOn, SortDirection.Ascending, "Cherry,Apple,Banana")]
    [InlineData(ContractSortField.StartsOn, SortDirection.Descending, "Banana,Apple,Cherry")]
    [InlineData(ContractSortField.Product, SortDirection.Ascending, "Apple,Banana,Cherry")]
    [InlineData(ContractSortField.Product, SortDirection.Descending, "Cherry,Banana,Apple")]
    public void Contracts_are_sorted_by_the_field_and_in_the_direction_asked_for(ContractSortField field, SortDirection direction, string expected)
    {
        List<Contract> contracts =
        [
            Concluded("Apple", new DateOnly(2025, 6, 1), new DateOnly(2027, 5, 31)),
            Concluded("Banana", new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30)),
            Concluded("Cherry", new DateOnly(2024, 1, 1), new DateOnly(2026, 12, 31)),
        ];

        var sorted = contracts.AsQueryable().SortedBy(new Sorting<ContractSortField>(field, direction));

        sorted.Select(contract => contract.Product).ShouldBe(expected.Split(','));
    }

    private static Contract Concluded(string product, DateOnly startsOn, DateOnly endsOn) =>
        Contract.Conclude(ContractId.New(), product, startsOn, endsOn, ContractRenewal.Renews).Value;
}
