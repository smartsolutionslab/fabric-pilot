using FabricPilot.Modules.Tickets.Domain.Tickets;
using FabricPilot.Modules.Tickets.Domain.Tickets.Events;

namespace FabricPilot.Modules.Tickets.Tests.Tickets;

public class TicketTests
{
    private static readonly DateTimeOffset RaisedAt = new(2026, 11, 12, 9, 30, 0, TimeSpan.Zero);

    [Fact]
    public void A_ticket_is_raised_with_its_subject_description_priority_and_attachments()
    {
        var id = TicketId.New();
        var screenshot = File("screenshot.png", 2048, "image/png");
        var log = File("app.log", 512);

        var result = Raise(id, "  Printer offline  ", "  It shows error 42.  ", TicketPriority.High, [screenshot, log]);

        result.IsSuccess.ShouldBeTrue();
        var ticket = result.Value;
        ticket.Id.ShouldBe(id);
        ticket.Subject.ShouldBe("Printer offline");
        ticket.Description.ShouldBe("It shows error 42.");
        ticket.Priority.ShouldBe(TicketPriority.High);
        ticket.RaisedAt.ShouldBe(RaisedAt);
        ticket.Attachments.Select(attachment => (attachment.Id, attachment.FileName, attachment.ContentType, attachment.Size))
            .ShouldBe([(screenshot.Id, "screenshot.png", "image/png", 2048L), (log.Id, "app.log", "application/octet-stream", 512L)]);
        ticket.DomainEvents.ShouldHaveSingleItem().ShouldBe(new TicketRaised(id));
    }

    [Fact]
    public void A_ticket_without_attachments_is_raised()
    {
        var result = Raise(TicketId.New(), "Question", "How do I reset my password?", TicketPriority.Low, []);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Attachments.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_ticket_without_a_subject_is_refused(string subject)
    {
        var result = Raise(TicketId.New(), subject, "Description", TicketPriority.Normal, []);

        result.IsFailure.ShouldBeTrue();
        result.Error.Value.ShouldBeOfType<InvalidSubject>();
    }

    [Fact]
    public void A_subject_of_the_maximum_length_is_accepted_and_one_longer_is_refused()
    {
        Raise(TicketId.New(), new string('s', Ticket.MaximumSubjectLength), "Description", TicketPriority.Normal, []).IsSuccess.ShouldBeTrue();

        var result = Raise(TicketId.New(), new string('s', Ticket.MaximumSubjectLength + 1), "Description", TicketPriority.Normal, []);

        result.IsFailure.ShouldBeTrue();
        result.Error.Value.ShouldBeOfType<InvalidSubject>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_ticket_without_a_description_is_refused(string description)
    {
        var result = Raise(TicketId.New(), "Subject", description, TicketPriority.Normal, []);

        result.IsFailure.ShouldBeTrue();
        result.Error.Value.ShouldBeOfType<InvalidDescription>();
    }

    [Fact]
    public void A_description_longer_than_the_maximum_is_refused()
    {
        var result = Raise(TicketId.New(), "Subject", new string('d', Ticket.MaximumDescriptionLength + 1), TicketPriority.Normal, []);

        result.IsFailure.ShouldBeTrue();
        result.Error.Value.ShouldBeOfType<InvalidDescription>();
    }

    [Fact]
    public void A_file_of_exactly_the_size_limit_is_accepted()
    {
        var result = Raise(TicketId.New(), "Subject", "Description", TicketPriority.Normal, [File("big.bin", Attachment.MaximumSize)]);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void A_file_over_the_size_limit_is_refused_naming_the_file()
    {
        var result = Raise(TicketId.New(), "Subject", "Description", TicketPriority.Normal, [File("small.txt", 10), File("huge.iso", Attachment.MaximumSize + 1)]);

        result.IsFailure.ShouldBeTrue();
        var tooLarge = result.Error.Value.ShouldBeOfType<AttachmentTooLarge>();
        tooLarge.FileName.ShouldBe("huge.iso");
        tooLarge.Message.ShouldContain("huge.iso");
        tooLarge.Message.ShouldContain("10 MB");
    }

    [Fact]
    public void More_attachments_than_the_maximum_are_refused()
    {
        var files = Enumerable.Range(1, Ticket.MaximumAttachments + 1).Select(number => File($"file{number}.txt", 1)).ToList();

        var result = Raise(TicketId.New(), "Subject", "Description", TicketPriority.Normal, files);

        result.IsFailure.ShouldBeTrue();
        result.Error.Value.ShouldBeOfType<TooManyAttachments>();
    }

    [Theory]
    [InlineData(@"C:\fakepath\report.pdf", "report.pdf")]
    [InlineData("folder/sub/report.pdf", "report.pdf")]
    [InlineData("report.pdf", "report.pdf")]
    public void An_attachment_keeps_only_the_last_part_of_its_file_name(string given, string kept)
    {
        var result = Raise(TicketId.New(), "Subject", "Description", TicketPriority.Normal, [File(given, 1)]);

        result.Value.Attachments.ShouldHaveSingleItem().FileName.ShouldBe(kept);
    }

    [Theory]
    [InlineData("")]
    [InlineData("folder/")]
    public void An_attachment_without_a_file_name_is_refused(string fileName)
    {
        var result = Raise(TicketId.New(), "Subject", "Description", TicketPriority.Normal, [File(fileName, 1)]);

        result.IsFailure.ShouldBeTrue();
        result.Error.Value.ShouldBeOfType<InvalidAttachmentName>();
    }

    [Fact]
    public void An_attachment_whose_file_name_is_too_long_is_refused()
    {
        var result = Raise(TicketId.New(), "Subject", "Description", TicketPriority.Normal, [File(new string('n', Attachment.MaximumFileNameLength + 1), 1)]);

        result.IsFailure.ShouldBeTrue();
        result.Error.Value.ShouldBeOfType<InvalidAttachmentName>();
    }

    [Fact]
    public void An_attachment_without_a_content_type_is_stored_as_octet_stream()
    {
        var result = Raise(TicketId.New(), "Subject", "Description", TicketPriority.Normal, [new AttachmentFile(AttachmentId.New(), "notes.txt", " ", 3)]);

        result.Value.Attachments.ShouldHaveSingleItem().ContentType.ShouldBe("application/octet-stream");
    }

    private static AttachmentFile File(string name, long size, string contentType = "application/octet-stream") => new(AttachmentId.New(), name, contentType, size);

    private static Result<Ticket, TicketError> Raise(TicketId id, string subject, string description, TicketPriority priority, IReadOnlyList<AttachmentFile> attachments) =>
        Ticket.Raise(id, subject, description, priority, attachments, RaisedAt);
}
