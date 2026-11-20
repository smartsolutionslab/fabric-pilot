using System.Text.Json.Serialization;

namespace FabricPilot.Modules.Tickets.Domain.Tickets;

/// <summary>How urgent a ticket is. Travels by name.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<TicketPriority>))]
public enum TicketPriority
{
    Low,
    Normal,
    High,
    Urgent,
}
