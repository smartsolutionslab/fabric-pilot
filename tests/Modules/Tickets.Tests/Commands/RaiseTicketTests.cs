using System.Net;
using System.Text;
using FabricPilot.Modules.Tickets.Application;
using FabricPilot.Modules.Tickets.Application.Commands.RaiseTicket;
using FabricPilot.Modules.Tickets.Domain.Tickets;
using Microsoft.Extensions.Time.Testing;

namespace FabricPilot.Modules.Tickets.Tests.Commands;

public class RaiseTicketTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 12, 10, 0, 0, TimeSpan.Zero);

    private readonly InMemoryTicketStore tickets = new();
    private readonly InMemoryBlobStore blobs = new();

    [Fact]
    public async Task A_raised_ticket_is_saved_with_its_attachments_and_each_file_is_uploaded_unchanged()
    {
        var screenshot = Encoding.UTF8.GetBytes("a screenshot");
        var log = Encoding.UTF8.GetBytes("a log file");

        var result = await Handle(new RaiseTicket("Printer offline", "Error 42.", "Urgent", [File("screenshot.png", "image/png", screenshot), File("app.log", "text/plain", log)]));

        result.IsSuccess.ShouldBeTrue();
        var ticket = tickets.Saved.ShouldHaveSingleItem();
        ticket.Id.Value.ShouldBe(result.Value);
        ticket.Subject.ShouldBe("Printer offline");
        ticket.Priority.ShouldBe(TicketPriority.Urgent);
        ticket.RaisedAt.ShouldBe(Now);
        ticket.Attachments.Select(attachment => attachment.FileName).ShouldBe(["screenshot.png", "app.log"]);
        blobs.Blobs[AttachmentBlobs.Name(ticket.Id, ticket.Attachments[0].Id)].ShouldBe(screenshot);
        blobs.Blobs[AttachmentBlobs.Name(ticket.Id, ticket.Attachments[1].Id)].ShouldBe(log);
        blobs.Blobs.Count.ShouldBe(2);
    }

    [Fact]
    public async Task A_file_over_the_size_limit_is_refused_and_nothing_is_uploaded_or_saved()
    {
        var oversized = new NewAttachment("huge.iso", "application/octet-stream", Attachment.MaximumSize + 1, Stream.Null);

        var result = await Handle(new RaiseTicket("Subject", "Description", "Normal", [File("fine.txt", "text/plain", [1, 2, 3]), oversized]));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("tickets.attachment-too-large");
        result.Error.Status.ShouldBe(HttpStatusCode.UnprocessableEntity);
        blobs.Blobs.ShouldBeEmpty();
        tickets.Saved.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("Critical")]
    [InlineData("high")]
    [InlineData("1")]
    [InlineData("")]
    public async Task A_priority_that_is_not_one_of_the_named_ones_is_refused(string priority)
    {
        var result = await Handle(new RaiseTicket("Subject", "Description", priority, []));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("tickets.unknown-priority");
        result.Error.Status.ShouldBe(HttpStatusCode.UnprocessableEntity);
        tickets.Saved.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_ticket_without_a_subject_is_refused_with_the_domains_code()
    {
        var result = await Handle(new RaiseTicket(" ", "Description", "Low", []));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("tickets.invalid-subject");
        result.Error.Status.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    private static NewAttachment File(string fileName, string contentType, byte[] content) =>
        new(fileName, contentType, content.Length, new MemoryStream(content));

    private Task<Result<Guid, ApiError>> Handle(RaiseTicket command) =>
        new RaiseTicketHandler(tickets, blobs, new FakeTimeProvider(Now)).HandleAsync(command, TestContext.Current.CancellationToken);
}
