using System.Net;

namespace FabricPilot.Modules.Tickets.Application;

public sealed record TicketNotFound(Guid Ticket)
    : ApiError("tickets.ticket-not-found", $"There is no ticket {Ticket}.", HttpStatusCode.NotFound);
