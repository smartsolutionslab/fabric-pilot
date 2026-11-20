namespace FabricPilot.Contracts.Tickets;

/// <summary>
/// Staff assigned a ticket, or assigned it to someone else. Published through the tickets module's unit of work, in the
/// ticket's tenant.
/// </summary>
/// <param name="TicketId">The ticket assigned.</param>
/// <param name="Subject">The ticket's subject, as its customer wrote it.</param>
/// <param name="Assignee">The user name of the staff member it is assigned to now.</param>
public sealed record TicketAssignedV1(Guid TicketId, string Subject, string Assignee) : IntegrationEvent;
