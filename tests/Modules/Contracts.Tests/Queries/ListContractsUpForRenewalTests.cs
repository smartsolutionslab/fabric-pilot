using FabricPilot.Modules.Contracts.Application.Queries.ListContractsUpForRenewal;
using FabricPilot.Modules.Contracts.Domain.Contracts;
using Microsoft.Extensions.Time.Testing;

namespace FabricPilot.Modules.Contracts.Tests.Queries;

public class ListContractsUpForRenewalTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 12, 23, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task The_list_shows_the_contracts_that_renew_within_the_window_from_today_the_soonest_first()
    {
        var store = new InMemoryContractStore();
        var later = Renewing("Hosting", new DateOnly(2027, 2, 10));
        var sooner = Renewing("Managed printing", new DateOnly(2026, 12, 1));
        var outsideTheWindow = Renewing("Backup", new DateOnly(2027, 2, 11));
        var ending = Contract.Conclude(ContractId.New(), "Migration project", new DateOnly(2026, 3, 1), new DateOnly(2026, 11, 30), ContractRenewal.Ends).Value;
        store.Saved.AddRange([later, outsideTheWindow, ending, sooner]);

        var result = await Handler(store).HandleAsync(new ListContractsUpForRenewal(Page.First, PageSize.Default), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.TotalCount.ShouldBe(2);
        result.Value.Items.Select(contract => contract.Id).ShouldBe([sooner.Id.Value, later.Id.Value]);
        result.Value.Items[0].RenewsOn.ShouldBe(new DateOnly(2026, 12, 1));
    }

    [Fact]
    public async Task Today_is_the_clocks_date()
    {
        var store = new InMemoryContractStore();

        await Handler(store).HandleAsync(new ListContractsUpForRenewal(Page.First, PageSize.Default), TestContext.Current.CancellationToken);

        store.AskedForRenewalsOn.ShouldBe(new DateOnly(2026, 11, 12));
    }

    [Fact]
    public async Task The_list_shows_one_page()
    {
        var store = new InMemoryContractStore();
        store.Saved.AddRange([Renewing("Hosting", new DateOnly(2026, 12, 1)), Renewing("Backup", new DateOnly(2026, 12, 2))]);
        PageSize.TryFrom(1, out var one).ShouldBeTrue();
        Page.TryFrom(2, out var second).ShouldBeTrue();

        var result = await Handler(store).HandleAsync(new ListContractsUpForRenewal(second, one), TestContext.Current.CancellationToken);

        result.Value.TotalCount.ShouldBe(2);
        result.Value.Page.ShouldBe(2);
        result.Value.Items.ShouldHaveSingleItem().Product.ShouldBe("Backup");
    }

    private static ListContractsUpForRenewalHandler Handler(InMemoryContractStore store) => new(store, new FakeTimeProvider(Now));

    private static Contract Renewing(string product, DateOnly renewsOn) =>
        Contract.Conclude(ContractId.New(), product, renewsOn.AddYears(-1), renewsOn.AddDays(-1), ContractRenewal.Renews).Value;
}
