namespace FabricPilot.Modules.Tickets.Domain.Tickets;

/// <summary>The customer who raised a ticket: their identity provider's user id (<c>sub</c>) and their e-mail address.</summary>
public sealed record TicketCustomer(string UserId, string Email)
{
    public const int MaximumUserIdLength = 64;

    public const int MaximumEmailLength = 320;
}
