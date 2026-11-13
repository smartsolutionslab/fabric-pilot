using FabricPilot.BuildingBlocks.Tenancy;
using FabricPilot.Contracts.Tickets;
using FabricPilot.Modules.Notifications.Domain.Notices;

namespace FabricPilot.Modules.Notifications.Application.Consumers;

/// <summary>
/// Tells the customer who raised a ticket that its status changed: a notice in the portal and an e-mail, once per event.
/// The notice is saved before the e-mail is sent and marked e-mailed after, so a redelivered event finds it and sends
/// nothing, and one whose e-mail failed sends it then. Only a failure between sending and marking sends it twice.
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
        if (!found.HasValue)
        {
            notices.Add(notice);
            await notices.SaveAsync(cancellationToken);
        }

        if (notice.IsEmailed)
        {
            return;
        }

        await email.SendAsync(
            new EmailMessage(
                customerEmail,
                $"Your ticket \"{subject}\" is now {status}",
                $"The status of your ticket \"{subject}\" changed from {previousStatus} to {status}.\n\nSee it in the portal."),
            cancellationToken);
        notice.MarkEmailed(time.GetUtcNow());
        await notices.SaveAsync(cancellationToken);
    }
}
