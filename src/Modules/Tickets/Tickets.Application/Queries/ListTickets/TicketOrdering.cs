using System.Diagnostics;
using System.Linq.Expressions;
using FabricPilot.Modules.Tickets.Domain.Tickets;

namespace FabricPilot.Modules.Tickets.Application.Queries.ListTickets;

public static class TicketOrdering
{
    /// <summary>Sorted as asked, then by id in the same direction, so tickets with the same key keep one order across pages.</summary>
    public static IOrderedQueryable<Ticket> SortedBy(this IQueryable<Ticket> tickets, Sorting<TicketSortField> sorting)
    {
        Ensure.That(tickets).IsNotNull();
        Ensure.That(sorting).IsNotNull();

        return sorting.Field switch
        {
            TicketSortField.RaisedAt => By(tickets, ticket => ticket.RaisedAt, sorting.Direction),
            TicketSortField.Subject => By(tickets, ticket => ticket.Subject, sorting.Direction),
            _ => throw new UnreachableException($"The list query refuses the sort field {sorting.Field}."),
        };
    }

    private static IOrderedQueryable<Ticket> By<TKey>(IQueryable<Ticket> tickets, Expression<Func<Ticket, TKey>> key, SortDirection direction) =>
        direction == SortDirection.Ascending
            ? tickets.OrderBy(key).ThenBy(ticket => ticket.Id)
            : tickets.OrderByDescending(key).ThenByDescending(ticket => ticket.Id);
}
