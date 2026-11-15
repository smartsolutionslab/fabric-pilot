using FabricPilot.Modules.Tickets.Application.DTOs;
using FabricPilot.Modules.Tickets.Application.Queries.ListTriageTickets;
using FabricPilot.Modules.Tickets.Application.Queries.ListTickets;
using FabricPilot.Modules.Tickets.Domain.Tickets;
using SortDirection = FabricPilot.Kernel.SortDirection;

namespace FabricPilot.Modules.Tickets.Tests.Queries;

public class ListTriageTicketsTests
{
    private static readonly DateTimeOffset Monday = new(2026, 11, 9, 8, 0, 0, TimeSpan.Zero);

    private static readonly Sorting<TicketSortField> OldestFirst = new(TicketSortField.RaisedAt, SortDirection.Ascending);

    private readonly InMemoryTicketStore store = new();

    [Fact]
    public async Task The_queue_shows_every_tenants_tickets_with_their_tenant_status_and_assignee()
    {
        var printer = Raised("Printer offline", TicketPriority.High, Monday, "tenant-a");
        var invoice = Raised("Wrong invoice", TicketPriority.Low, Monday.AddHours(1), "tenant-b");
        invoice.ChangeStatus(TicketStatus.InProgress);
        invoice.AssignTo("staff");

        var result = await Handle(TicketQueueFilter.Everything, OldestFirst);

        result.IsSuccess.ShouldBeTrue();
        result.Value.TotalCount.ShouldBe(2);
        result.Value.Items.ShouldBe(
        [
            new QueuedTicketDto(printer.Id.Value, "tenant-a", "Printer offline", "High", "Open", null, Monday, 0),
            new QueuedTicketDto(invoice.Id.Value, "tenant-b", "Wrong invoice", "Low", "InProgress", "staff", Monday.AddHours(1), 0),
        ]);
    }

    [Fact]
    public async Task The_queue_is_filtered_by_tenant()
    {
        Raised("Printer offline", TicketPriority.High, Monday, "tenant-a");
        var invoice = Raised("Wrong invoice", TicketPriority.Low, Monday, "tenant-b");

        var result = await Handle(TicketQueueFilter.Everything with { Tenant = Option<string>.Some("tenant-b") }, OldestFirst);

        result.Value.Items.ShouldHaveSingleItem().Id.ShouldBe(invoice.Id.Value);
        result.Value.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task The_queue_is_filtered_by_status()
    {
        Raised("Printer offline", TicketPriority.High, Monday, "tenant-a");
        var resolved = Raised("Wrong invoice", TicketPriority.Low, Monday, "tenant-b");
        resolved.ChangeStatus(TicketStatus.Resolved);

        var result = await Handle(TicketQueueFilter.Everything with { Status = Option<TicketStatus>.Some(TicketStatus.Resolved) }, OldestFirst);

        result.Value.Items.ShouldHaveSingleItem().Id.ShouldBe(resolved.Id.Value);
    }

    [Fact]
    public async Task The_queue_is_filtered_by_priority()
    {
        var urgent = Raised("Printer on fire", TicketPriority.Urgent, Monday, "tenant-a");
        Raised("Wrong invoice", TicketPriority.Low, Monday, "tenant-a");

        var result = await Handle(TicketQueueFilter.Everything with { Priority = Option<TicketPriority>.Some(TicketPriority.Urgent) }, OldestFirst);

        result.Value.Items.ShouldHaveSingleItem().Id.ShouldBe(urgent.Id.Value);
    }

    [Fact]
    public async Task The_filters_apply_together()
    {
        var wanted = Raised("Printer offline", TicketPriority.High, Monday, "tenant-b");
        Raised("Other tenant", TicketPriority.High, Monday, "tenant-a");
        Raised("Other priority", TicketPriority.Low, Monday, "tenant-b");
        Raised("Other status", TicketPriority.High, Monday, "tenant-b").ChangeStatus(TicketStatus.Closed);
        var filter = new TicketQueueFilter(
            Option<string>.Some("tenant-b"),
            Option<TicketStatus>.Some(TicketStatus.Open),
            Option<TicketPriority>.Some(TicketPriority.High));

        var result = await Handle(filter, OldestFirst);

        result.Value.Items.ShouldHaveSingleItem().Id.ShouldBe(wanted.Id.Value);
    }

    [Fact]
    public async Task The_queue_is_sorted_by_age_either_way()
    {
        var older = Raised("Older", TicketPriority.Normal, Monday, "tenant-a");
        var newer = Raised("Newer", TicketPriority.Normal, Monday.AddDays(1), "tenant-b");

        var oldestFirst = await Handle(TicketQueueFilter.Everything, OldestFirst);
        var newestFirst = await Handle(TicketQueueFilter.Everything, ListTickets.NewestFirst);

        oldestFirst.Value.Items.Select(item => item.Id).ShouldBe([older.Id.Value, newer.Id.Value]);
        newestFirst.Value.Items.Select(item => item.Id).ShouldBe([newer.Id.Value, older.Id.Value]);
    }

    [Theory]
    [InlineData(99, 0)]
    [InlineData(0, 99)]
    public async Task A_status_or_priority_that_is_not_declared_is_refused(int status, int priority)
    {
        var filter = new TicketQueueFilter(Option<string>.None, Option<TicketStatus>.Some((TicketStatus)status), Option<TicketPriority>.Some((TicketPriority)priority));

        await Should.ThrowAsync<GuardException>(() => Handle(filter, OldestFirst));
    }

    [Fact]
    public async Task A_sort_field_the_query_does_not_declare_is_refused()
    {
        await Should.ThrowAsync<GuardException>(() => Handle(TicketQueueFilter.Everything, new Sorting<TicketSortField>((TicketSortField)99, SortDirection.Ascending)));
    }

    private Ticket Raised(string subject, TicketPriority priority, DateTimeOffset at, string tenant)
    {
        var ticket = Ticket.Raise(TicketId.New(), subject, "Description", priority, Customers.Developer, [], at).Value;
        store.Saved.Add(ticket);
        store.Tenants[ticket.Id] = tenant;
        return ticket;
    }

    private Task<Result<PagedResult<QueuedTicketDto>, ApiError>> Handle(TicketQueueFilter filter, Sorting<TicketSortField> sorting) =>
        new ListTriageTicketsHandler(store).HandleAsync(new ListTriageTickets(Page.First, PageSize.Default, sorting, filter), TestContext.Current.CancellationToken);
}
