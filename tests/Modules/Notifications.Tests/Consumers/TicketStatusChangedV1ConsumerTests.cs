using FabricPilot.Contracts;
using FabricPilot.Contracts.Tickets;
using FabricPilot.Modules.Notifications.Application.Consumers;
using Microsoft.Extensions.Time.Testing;

namespace FabricPilot.Modules.Notifications.Tests.Consumers;

public class TicketStatusChangedV1ConsumerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 13, 9, 30, 0, TimeSpan.Zero);

    private readonly InMemoryNoticeStore notices = new();

    private readonly RecordingEmailSender email = new();

    private readonly FakeTimeProvider time = new(Now);

    [Fact]
    public async Task A_status_change_gives_the_customer_who_raised_the_ticket_a_notice_and_an_email()
    {
        var changed = StatusChanged("tenant-a");

        await Consumer().HandleAsync(changed, TestContext.Current.CancellationToken);

        var (tenant, notice) = notices.Saved.ShouldHaveSingleItem();
        tenant.ShouldBe("tenant-a");
        notice.Recipient.ShouldBe("customer-1");
        notice.TicketId.ShouldBe(changed.TicketId);
        notice.CausingEvent.ShouldBe(changed.Metadata.EventIdentifier);
        notice.Text.ShouldBe("Your ticket \"Printer offline\" is now Resolved.");
        notice.CreatedAt.ShouldBe(Now);
        notice.EmailedAt.ShouldBe(Now);
        var sent = email.Sent.ShouldHaveSingleItem();
        sent.To.ShouldBe("customer-1@example.com");
        sent.Subject.ShouldBe("Your ticket \"Printer offline\" is now Resolved");
        sent.Body.ShouldContain("Printer offline");
        sent.Body.ShouldContain("from Open to Resolved");
    }

    [Fact]
    public async Task The_same_event_delivered_twice_sends_one_email_and_keeps_one_notice()
    {
        var changed = StatusChanged("tenant-a");

        await Consumer().HandleAsync(changed, TestContext.Current.CancellationToken);
        await Consumer().HandleAsync(changed, TestContext.Current.CancellationToken);

        notices.Saved.ShouldHaveSingleItem();
        email.Sent.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task A_redelivery_after_the_email_failed_sends_it_and_still_keeps_one_notice()
    {
        var changed = StatusChanged("tenant-a");
        email.FailsNextSend = true;

        await Should.ThrowAsync<IOException>(() => Consumer().HandleAsync(changed, TestContext.Current.CancellationToken));
        notices.Saved.ShouldHaveSingleItem().Notice.IsEmailed.ShouldBeFalse();
        email.Sent.ShouldBeEmpty();

        await Consumer().HandleAsync(changed, TestContext.Current.CancellationToken);

        notices.Saved.ShouldHaveSingleItem().Notice.IsEmailed.ShouldBeTrue();
        email.Sent.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task A_copy_delivered_while_the_first_is_still_sending_sends_nothing_and_is_retried()
    {
        var changed = StatusChanged("tenant-a");
        Exception? overlapping = null;
        email.WhileSending = async () => overlapping = await Record.ExceptionAsync(() => Consumer().HandleAsync(changed, TestContext.Current.CancellationToken));

        await Consumer().HandleAsync(changed, TestContext.Current.CancellationToken);

        overlapping.ShouldBeOfType<InvalidOperationException>();
        email.Sent.ShouldHaveSingleItem();
        await Consumer().HandleAsync(changed, TestContext.Current.CancellationToken);
        email.Sent.ShouldHaveSingleItem();
        notices.Saved.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Two_status_changes_of_one_ticket_are_two_notices_and_two_emails()
    {
        await Consumer().HandleAsync(StatusChanged("tenant-a"), TestContext.Current.CancellationToken);
        await Consumer().HandleAsync(StatusChanged("tenant-a"), TestContext.Current.CancellationToken);

        notices.Saved.Count.ShouldBe(2);
        email.Sent.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Each_notice_belongs_to_the_tenant_that_published_its_event()
    {
        await Consumer().HandleAsync(StatusChanged("tenant-a"), TestContext.Current.CancellationToken);
        await Consumer().HandleAsync(StatusChanged("tenant-b"), TestContext.Current.CancellationToken);

        notices.Saved.Select(row => row.Tenant).ShouldBe(["tenant-a", "tenant-b"]);
    }

    private static TicketStatusChangedV1 StatusChanged(string tenant) =>
        new(Guid.CreateVersion7(), "Printer offline", "Open", "Resolved", "customer-1", "customer-1@example.com")
        {
            Metadata = new EventMetadata(Guid.CreateVersion7(), Now, tenant),
        };

    private TicketStatusChangedV1Consumer Consumer() => new(notices, email, time);
}
