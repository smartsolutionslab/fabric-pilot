using FabricPilot.BuildingBlocks.Tenancy;
using FabricPilot.Contracts.Tickets;
using FabricPilot.Modules.Notifications.Domain.Notices;

namespace FabricPilot.Modules.Notifications.Application.Consumers;

/// <summary>
/// Tells the customer who raised a ticket that its status changed: a notice in the portal and an e-mail, once per event.
/// A delivery claims the e-mail on the notice and saves the claim before sending; the store saves only over the version
/// it read, so of two copies handled at once one claims and the other fails and is retried, by when the e-mail is sent.
/// A send still running <see cref="Notice.EmailSendDeadline"/> after the claim is cancelled, well inside the claim's lease.
/// A send that fails gives the claim back; a delivery that dies holding it blocks others for
/// <see cref="Notice.EmailClaimLease"/>. A second e-mail goes out only if the host dies between sending and marking it
/// sent.
/// </summary>
public sealed class TicketStatusChangedV1Consumer(INoticeStore notices, IEmailSender email, TimeProvider time)
{
    public async Task HandleAsync(TicketStatusChangedV1 message, CancellationToken cancellationToken)
    {
        Ensure.That(message).IsNotNull();

        using var tenant = TenantScope.Begin(message.Metadata);
        var (ticketId, subject, previousStatus, status, customerUserId, customerEmail) = message;
        var eventIdentifier = message.Metadata.EventIdentifier;

        var found = await notices.FindCausedByAsync(eventIdentifier, cancellationToken);
        var notice = found.HasValue
            ? found.Value
            : Notice.OfStatusChange(NoticeId.New(), eventIdentifier, customerUserId, ticketId, subject, status, time.GetUtcNow());
        if (notice.IsEmailed)
        {
            return;
        }

        var claimedAt = time.GetUtcNow();
        if (!notice.TryClaimEmail(claimedAt))
        {
            // Thrown to be retried: by then the delivery holding the claim has sent the e-mail, or given the claim back.
            throw new InvalidOperationException($"Another delivery of event {eventIdentifier} is e-mailing its notice.");
        }

        if (!found.HasValue)
        {
            notices.Add(notice);
        }

        await notices.SaveAsync(cancellationToken);
        var left = claimedAt + Notice.EmailSendDeadline - time.GetUtcNow();
        using var deadline = new CancellationTokenSource(left > TimeSpan.Zero ? left : TimeSpan.Zero, time);
        using var sending = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, deadline.Token);
        try
        {
            await email.SendAsync(
                new EmailMessage(
                    customerEmail,
                    $"Your ticket \"{subject}\" is now {status}",
                    $"The status of your ticket \"{subject}\" changed from {previousStatus} to {status}.\n\nSee it in the portal."),
                sending.Token);
        }
        catch
        {
            notice.ReleaseEmailClaim();
            await notices.SaveAsync(CancellationToken.None);
            throw;
        }

        notice.MarkEmailed(time.GetUtcNow());
        await notices.SaveAsync(cancellationToken);
    }
}
