using System.Text.Json.Serialization;

namespace FabricPilot.Modules.Tickets.Application.Queries.ListTickets;

/// <summary>What the ticket list can be sorted by.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<TicketSortField>))]
public enum TicketSortField
{
    RaisedAt,
    Subject,
}
