using System.Net;
using FabricPilot.Modules.Tickets.Domain.Tickets;

namespace FabricPilot.Modules.Tickets.Application;

public sealed record UnknownPriority(string Priority)
    : ApiError("tickets.unknown-priority", $"'{Priority}' is not a priority; use one of {string.Join(", ", Enum.GetNames<TicketPriority>())}.", HttpStatusCode.UnprocessableEntity);
