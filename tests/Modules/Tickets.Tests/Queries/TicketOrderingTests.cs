using FabricPilot.Modules.Tickets.Application.Queries.ListTickets;
using FabricPilot.Modules.Tickets.Domain.Tickets;
using SortDirection = FabricPilot.Kernel.SortDirection;

namespace FabricPilot.Modules.Tickets.Tests.Queries;

public class TicketOrderingTests
{
    [Theory]
    [InlineData(TicketSortField.RaisedAt, SortDirection.Ascending, "Banana,Cherry,Apple")]
    [InlineData(TicketSortField.RaisedAt, SortDirection.Descending, "Apple,Cherry,Banana")]
    [InlineData(TicketSortField.Subject, SortDirection.Ascending, "Apple,Banana,Cherry")]
    [InlineData(TicketSortField.Subject, SortDirection.Descending, "Cherry,Banana,Apple")]
    public void Tickets_are_sorted_by_the_field_and_in_the_direction_asked_for(TicketSortField field, SortDirection direction, string expected)
    {
        List<Ticket> tickets =
        [
            Raised("Apple", new DateTimeOffset(2026, 11, 3, 8, 0, 0, TimeSpan.Zero)),
            Raised("Banana", new DateTimeOffset(2026, 11, 1, 8, 0, 0, TimeSpan.Zero)),
            Raised("Cherry", new DateTimeOffset(2026, 11, 2, 8, 0, 0, TimeSpan.Zero)),
        ];

        var sorted = tickets.AsQueryable().SortedBy(new Sorting<TicketSortField>(field, direction));

        sorted.Select(ticket => ticket.Subject).ShouldBe(expected.Split(','));
    }

    private static Ticket Raised(string subject, DateTimeOffset at) =>
        Ticket.Raise(TicketId.New(), subject, "Description", TicketPriority.Normal, [], at).Value;
}
