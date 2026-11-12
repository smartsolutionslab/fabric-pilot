using FabricPilot.Modules.Contracts.Application.Queries.ListContracts;
using FabricPilot.Modules.Contracts.Domain.Contracts;
using SortDirection = FabricPilot.Kernel.SortDirection;

namespace FabricPilot.Modules.Contracts.Tests.Queries;

public class ListContractsTests
{
    [Fact]
    public async Task The_list_shows_one_page_of_contracts_the_next_to_end_first_with_their_renewal_dates()
    {
        var store = new InMemoryContractStore();
        var later = Concluded("Hosting", new DateOnly(2026, 1, 1), new DateOnly(2027, 6, 30), ContractRenewal.Renews);
        var sooner = Concluded("Managed printing", new DateOnly(2025, 1, 1), new DateOnly(2026, 12, 31), ContractRenewal.Renews);
        store.Saved.AddRange([later, sooner]);
        PageSize.TryFrom(1, out var one).ShouldBeTrue();

        var result = await new ListContractsHandler(store).HandleAsync(new ListContracts(Page.First, one, ListContracts.EndingFirst), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.TotalCount.ShouldBe(2);
        result.Value.Page.ShouldBe(1);
        result.Value.PageSize.ShouldBe(1);
        var summary = result.Value.Items.ShouldHaveSingleItem();
        summary.Id.ShouldBe(sooner.Id.Value);
        summary.Product.ShouldBe("Managed printing");
        summary.StartsOn.ShouldBe(new DateOnly(2025, 1, 1));
        summary.EndsOn.ShouldBe(new DateOnly(2026, 12, 31));
        summary.RenewsOn.ShouldBe(new DateOnly(2027, 1, 1));
    }

    [Fact]
    public async Task A_contract_that_ends_without_renewing_is_listed_without_a_renewal_date()
    {
        var store = new InMemoryContractStore();
        store.Saved.Add(Concluded("Migration project", new DateOnly(2026, 3, 1), new DateOnly(2026, 8, 31), ContractRenewal.Ends));

        var result = await new ListContractsHandler(store).HandleAsync(new ListContracts(Page.First, PageSize.Default, ListContracts.EndingFirst), TestContext.Current.CancellationToken);

        var summary = result.Value.Items.ShouldHaveSingleItem();
        summary.EndsOn.ShouldBe(new DateOnly(2026, 8, 31));
        summary.RenewsOn.ShouldBeNull();
    }

    [Fact]
    public void The_next_contract_to_end_comes_first_by_default()
    {
        ListContracts.EndingFirst.ShouldBe(new Sorting<ContractSortField>(ContractSortField.EndsOn, SortDirection.Ascending));
    }

    [Theory]
    [InlineData(99, SortDirection.Ascending)]
    [InlineData((int)ContractSortField.Product, (SortDirection)7)]
    public async Task A_sort_field_or_direction_the_query_does_not_declare_is_refused(int field, SortDirection direction)
    {
        var sorting = new Sorting<ContractSortField>((ContractSortField)field, direction);

        await Should.ThrowAsync<GuardException>(() =>
            new ListContractsHandler(new InMemoryContractStore()).HandleAsync(new ListContracts(Page.First, PageSize.Default, sorting), TestContext.Current.CancellationToken));
    }

    private static Contract Concluded(string product, DateOnly startsOn, DateOnly endsOn, ContractRenewal renewal) =>
        Contract.Conclude(ContractId.New(), product, startsOn, endsOn, renewal).Value;
}
