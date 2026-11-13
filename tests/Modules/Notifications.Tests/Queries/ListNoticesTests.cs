using FabricPilot.BuildingBlocks.Tenancy;
using FabricPilot.Modules.Notifications.Application.Queries.ListNotices;
using FabricPilot.Modules.Notifications.Domain.Notices;

namespace FabricPilot.Modules.Notifications.Tests.Queries;

public class ListNoticesTests
{
    private static readonly DateTimeOffset Morning = new(2026, 11, 13, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task A_customer_sees_their_own_notices_newest_first_one_page_at_a_time()
    {
        var store = new InMemoryNoticeStore();
        var older = Notice("customer-1", "Printer offline", Morning);
        var newer = Notice("customer-1", "VPN access", Morning.AddHours(1));
        using (TenantScope.Begin("tenant-a"))
        {
            store.Add(older);
            store.Add(newer);
            store.Add(Notice("customer-2", "Someone else's", Morning.AddHours(2)));
            await store.SaveAsync(TestContext.Current.CancellationToken);
        }

        PageSize.TryFrom(1, out var one).ShouldBeTrue();
        using var tenant = TenantScope.Begin("tenant-a");
        var result = await new ListNoticesHandler(store).HandleAsync(new ListNotices("customer-1", Page.First, one), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.TotalCount.ShouldBe(2);
        result.Value.Page.ShouldBe(1);
        result.Value.PageSize.ShouldBe(1);
        var shown = result.Value.Items.ShouldHaveSingleItem();
        shown.Id.ShouldBe(newer.Id.Value);
        shown.TicketId.ShouldBe(newer.TicketId);
        shown.Text.ShouldBe("Your ticket \"VPN access\" is now Resolved.");
        shown.CreatedAt.ShouldBe(newer.CreatedAt);
    }

    [Fact]
    public async Task A_customer_with_no_notices_sees_an_empty_page()
    {
        var result = await new ListNoticesHandler(new InMemoryNoticeStore()).HandleAsync(new ListNotices("customer-1", Page.First, PageSize.Default), TestContext.Current.CancellationToken);

        result.Value.Items.ShouldBeEmpty();
        result.Value.TotalCount.ShouldBe(0);
    }

    private static Notice Notice(string recipient, string ticketSubject, DateTimeOffset createdAt) =>
        Domain.Notices.Notice.OfStatusChange(NoticeId.New(), Guid.CreateVersion7(), recipient, Guid.CreateVersion7(), ticketSubject, "Resolved", createdAt);
}
