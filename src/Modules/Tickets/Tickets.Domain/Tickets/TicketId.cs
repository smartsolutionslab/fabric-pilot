namespace FabricPilot.Modules.Tickets.Domain.Tickets;

public sealed record TicketId(Guid Value)
{
    public static TicketId New() => new(Guid.CreateVersion7());
}
