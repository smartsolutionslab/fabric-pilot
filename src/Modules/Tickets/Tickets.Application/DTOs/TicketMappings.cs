using System.Linq.Expressions;
using FabricPilot.Modules.Tickets.Domain.Tickets;

namespace FabricPilot.Modules.Tickets.Application.DTOs;

public static class TicketMappings
{
    public static TicketDto ToTicketDto(this Ticket ticket)
    {
        Ensure.That(ticket).IsNotNull();
        return new TicketDto(ticket.Id.Value, ticket.Subject, ticket.Description, ticket.Priority.ToString(), ticket.RaisedAt, [.. ticket.Attachments.Select(attachment => attachment.ToDto())]);
    }

    /// <summary>For an EF <c>Select</c>: the attachments are counted in the database, never read.</summary>
    public static Expression<Func<Ticket, TicketSummaryDto>> ToTicketSummaryDtoExpression() =>
        ticket => new TicketSummaryDto(ticket.Id.Value, ticket.Subject, ticket.Priority.ToString(), ticket.RaisedAt, ticket.Attachments.Count);
}
