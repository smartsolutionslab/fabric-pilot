using FabricPilot.BuildingBlocks.Domain;

namespace FabricPilot.Modules.Notifications.Domain.Notices;

/// <summary>
/// What a customer is told in the portal, once per event that caused it; it also remembers whether the same news went
/// out by e-mail, so a redelivered event sends none.
/// </summary>
public sealed class Notice : AggregateRoot<NoticeId>
{
    /// <summary>
    /// How long a delivery that claimed the e-mail has to send it before another may: longer than a send takes, and
    /// shorter than a message's retries last, so a copy retried after a crash still sends.
    /// </summary>
    public static readonly TimeSpan EmailClaimLease = TimeSpan.FromSeconds(45);

    /// <summary>
    /// How long after its claim a delivery may still be sending: a send still running then is cancelled, leaving the rest
    /// of the lease to give the claim back or mark the notice e-mailed before another delivery may claim it.
    /// </summary>
    public static readonly TimeSpan EmailSendDeadline = TimeSpan.FromSeconds(30);

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

    /// <summary>When a delivery claimed the e-mail to send it; none while no delivery is sending it.</summary>
    public DateTimeOffset? EmailClaimedAt { get; private set; }

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

    /// <summary>
    /// Claims the e-mail for the delivery about to send it: refused once it was sent, and while another delivery's claim
    /// is younger than <see cref="EmailClaimLease"/>.
    /// </summary>
    public bool TryClaimEmail(DateTimeOffset now)
    {
        if (IsEmailed || (EmailClaimedAt is { } claimedAt && now - claimedAt < EmailClaimLease))
        {
            return false;
        }

        EmailClaimedAt = now;
        return true;
    }

    /// <summary>Gives the claim back after a send that failed, so the next delivery sends at once.</summary>
    public void ReleaseEmailClaim() => EmailClaimedAt = null;

    public void MarkEmailed(DateTimeOffset emailedAt) => EmailedAt = emailedAt;
}
