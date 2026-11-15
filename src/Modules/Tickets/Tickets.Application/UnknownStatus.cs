using System.Net;
using FabricPilot.Modules.Tickets.Domain.Tickets;

namespace FabricPilot.Modules.Tickets.Application;

public sealed record UnknownStatus(string Requested)
    : ApiError("tickets.unknown-status", $"'{Requested}' is not a status; use one of {string.Join(", ", Enum.GetNames<TicketStatus>())}.", HttpStatusCode.UnprocessableEntity);
