namespace FabricPilot.Contracts.Tickets;

/// <summary>
/// A ticket's status changed; the customer who raised it is told. Published through the tickets module's unit of work
/// by the use case that changes a status (#7), in the ticket's tenant. Statuses travel by name.
/// </summary>
/// <param name="TicketId">The ticket whose status changed.</param>
/// <param name="Subject">The ticket's subject, as its customer wrote it.</param>
/// <param name="PreviousStatus">The status it had.</param>
/// <param name="Status">The status it has now.</param>
/// <param name="CustomerUserId">The Keycloak user id (<c>sub</c>) of the customer who raised the ticket.</param>
/// <param name="CustomerEmail">That customer's e-mail address.</param>
public sealed record TicketStatusChangedV1(
    Guid TicketId,
    string Subject,
    string PreviousStatus,
    string Status,
    string CustomerUserId,
    string CustomerEmail) : IntegrationEvent;
