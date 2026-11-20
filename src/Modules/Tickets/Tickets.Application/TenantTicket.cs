using FabricPilot.Modules.Tickets.Domain.Tickets;

namespace FabricPilot.Modules.Tickets.Application;

/// <summary>A ticket read across tenants, with the tenant it belongs to, which the persistence layer keeps beside it.</summary>
public sealed record TenantTicket(string Tenant, Ticket Ticket);
