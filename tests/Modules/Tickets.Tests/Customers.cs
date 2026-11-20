using FabricPilot.Modules.Tickets.Domain.Tickets;

namespace FabricPilot.Modules.Tickets.Tests;

/// <summary>Customers who raise the tests' tickets.</summary>
internal static class Customers
{
    public static TicketCustomer Developer { get; } = new("0f1e2d3c-4b5a-4968-8776-5a4b3c2d1e0f", "developer@example.com");
}
