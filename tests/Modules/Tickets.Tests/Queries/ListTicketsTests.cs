using FabricPilot.Modules.Tickets.Application.Queries.ListTickets;
using FabricPilot.Modules.Tickets.Domain.Tickets;
using SortDirection = FabricPilot.Kernel.SortDirection;

namespace FabricPilot.Modules.Tickets.Tests.Queries;

public class ListTicketsTests
{
    [Fact]
    public async Task The_list_shows_one_page_of_tickets_newest_first_with_their_attachment_counts()
    {
        var store = new InMemoryTicketStore();
        var older = Raised("Older", TicketPriority.Low, new DateTimeOffset(2026, 11, 1, 8, 0, 0, TimeSpan.Zero), 0);
        var newer = Raised("Newer", TicketPriority.High, new DateTimeOffset(2026, 11, 2, 8, 0, 0, TimeSpan.Zero), 2);
        store.Saved.AddRange([older, newer]);
        PageSize.TryFrom(1, out var one).ShouldBeTrue();

        var result = await new ListTicketsHandler(store).HandleAsync(new ListTickets(Page.First, one, ListTickets.NewestFirst), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.TotalCount.ShouldBe(2);
        result.Value.Page.ShouldBe(1);
        result.Value.PageSize.ShouldBe(1);
        var summary = result.Value.Items.ShouldHaveSingleItem();
        summary.Id.ShouldBe(newer.Id.Value);
        summary.Subject.ShouldBe("Newer");
        summary.Priority.ShouldBe("High");
        summary.RaisedAt.ShouldBe(newer.RaisedAt);
        summary.AttachmentCount.ShouldBe(2);
    }

    [Fact]
    public async Task The_list_is_sorted_as_the_query_asks()
    {
        var store = new InMemoryTicketStore();
        store.Saved.AddRange(
        [
            Raised("Banana", TicketPriority.Low, new DateTimeOffset(2026, 11, 1, 8, 0, 0, TimeSpan.Zero), 0),
            Raised("Apple", TicketPriority.Low, new DateTimeOffset(2026, 11, 2, 8, 0, 0, TimeSpan.Zero), 0),
        ]);
        var bySubject = new Sorting<TicketSortField>(TicketSortField.Subject, SortDirection.Ascending);

        var result = await new ListTicketsHandler(store).HandleAsync(new ListTickets(Page.First, PageSize.Default, bySubject), TestContext.Current.CancellationToken);

        result.Value.Items.Select(summary => summary.Subject).ShouldBe(["Apple", "Banana"]);
    }

    [Fact]
    public void Newest_first_is_the_default_order()
    {
        ListTickets.NewestFirst.ShouldBe(new Sorting<TicketSortField>(TicketSortField.RaisedAt, SortDirection.Descending));
    }

    [Theory]
    [InlineData(99, SortDirection.Ascending)]
    [InlineData((int)TicketSortField.Subject, (SortDirection)7)]
    public async Task A_sort_field_or_direction_the_query_does_not_declare_is_refused(int field, SortDirection direction)
    {
        var sorting = new Sorting<TicketSortField>((TicketSortField)field, direction);

        await Should.ThrowAsync<GuardException>(() =>
            new ListTicketsHandler(new InMemoryTicketStore()).HandleAsync(new ListTickets(Page.First, PageSize.Default, sorting), TestContext.Current.CancellationToken));
    }

    private static Ticket Raised(string subject, TicketPriority priority, DateTimeOffset at, int attachments) =>
        Ticket.Raise(
            TicketId.New(),
            subject,
            "Description",
            priority,
            [.. Enumerable.Range(1, attachments).Select(number => new AttachmentFile(AttachmentId.New(), $"file{number}.txt", "text/plain", 1))],
            at).Value;
}
