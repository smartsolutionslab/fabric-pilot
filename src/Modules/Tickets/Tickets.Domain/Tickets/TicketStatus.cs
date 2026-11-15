using System.Text.Json.Serialization;

namespace FabricPilot.Modules.Tickets.Domain.Tickets;

/// <summary>Where a ticket stands; staff change it, and its customer is told. Travels by name.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<TicketStatus>))]
public enum TicketStatus
{
    Open,
    InProgress,
    Resolved,
    Closed,
}
