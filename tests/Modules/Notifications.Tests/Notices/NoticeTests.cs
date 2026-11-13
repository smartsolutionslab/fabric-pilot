using FabricPilot.Modules.Notifications.Domain.Notices;

namespace FabricPilot.Modules.Notifications.Tests.Notices;

public class NoticeTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 11, 13, 9, 30, 0, TimeSpan.Zero);

    [Fact]
    public void A_status_change_notice_tells_its_recipient_which_ticket_is_in_which_status_now()
    {
        var id = NoticeId.New();
        var causingEvent = Guid.CreateVersion7();
        var ticketId = Guid.CreateVersion7();

        var notice = Notice.OfStatusChange(id, causingEvent, "customer-1", ticketId, "Printer offline", "Resolved", CreatedAt);

        notice.Id.ShouldBe(id);
        notice.CausingEvent.ShouldBe(causingEvent);
        notice.Recipient.ShouldBe("customer-1");
        notice.TicketId.ShouldBe(ticketId);
        notice.Text.ShouldBe("Your ticket \"Printer offline\" is now Resolved.");
        notice.CreatedAt.ShouldBe(CreatedAt);
        notice.IsEmailed.ShouldBeFalse();
    }

    [Fact]
    public void A_notice_marked_emailed_stays_emailed_from_that_time()
    {
        var notice = Notice.OfStatusChange(NoticeId.New(), Guid.CreateVersion7(), "customer-1", Guid.CreateVersion7(), "Printer offline", "Resolved", CreatedAt);

        notice.MarkEmailed(CreatedAt.AddSeconds(2));

        notice.IsEmailed.ShouldBeTrue();
        notice.EmailedAt.ShouldBe(CreatedAt.AddSeconds(2));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void A_notice_without_a_recipient_is_a_bug(string recipient)
    {
        Should.Throw<ArgumentException>(() => Notice.OfStatusChange(NoticeId.New(), Guid.CreateVersion7(), recipient, Guid.CreateVersion7(), "Printer offline", "Resolved", CreatedAt));
    }
}
