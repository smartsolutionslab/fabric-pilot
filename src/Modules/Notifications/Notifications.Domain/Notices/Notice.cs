using FabricPilot.BuildingBlocks.Domain;

namespace FabricPilot.Modules.Notifications.Domain.Notices;

/// <summary>
/// What a customer is told in the portal, once per event that caused it; it also remembers whether the same news went
/// out by e-mail, so a redelivered event sends none.
/// </summary>
public sealed class Notice : AggregateRoot<NoticeId>
{
    private Notice(NoticeId id, Guid causingEvent, string recipient, Guid ticketId, string text, DateTimeOffset createdAt)
        : base(id)
    {
        CausingEvent = causingEvent;
        Recipient = recipient;
        TicketId = ticketId;
        Text = text;
        CreatedAt = createdAt;
    }

    /// <summary>The integration event this notice tells of: one notice per event.</summary>
    public Guid CausingEvent { get; private set; }

    /// <summary>The Keycloak user id (<c>sub</c>) of the customer the notice is for.</summary>
    public string Recipient { get; private set; }

    public Guid TicketId { get; private set; }

    public string Text { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? EmailedAt { get; private set; }

    public bool IsEmailed => EmailedAt is not null;

    public static Notice OfStatusChange(
        NoticeId id,
        Guid causingEvent,
        string recipient,
        Guid ticketId,
        string ticketSubject,
        string status,
        DateTimeOffset createdAt)
    {
        Ensure.That(id).IsNotNull();
        Ensure.That(recipient).IsNotNullOrWhiteSpace();
        Ensure.That(ticketSubject).IsNotNull();
        Ensure.That(status).IsNotNullOrWhiteSpace();

        return new Notice(id, causingEvent, recipient, ticketId, $"Your ticket \"{ticketSubject}\" is now {status}.", createdAt);
    }

    public void MarkEmailed(DateTimeOffset emailedAt) => EmailedAt = emailedAt;
}
