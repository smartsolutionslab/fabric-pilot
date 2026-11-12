using System.Net;
using FabricPilot.Modules.Tickets.Application;
using FabricPilot.Modules.Tickets.Application.Queries.DownloadAttachment;
using FabricPilot.Modules.Tickets.Domain.Tickets;

namespace FabricPilot.Modules.Tickets.Tests.Queries;

public class DownloadAttachmentTests
{
    private readonly InMemoryTicketStore store = new();
    private readonly InMemoryBlobStore blobs = new();
    private readonly Ticket ticket;
    private readonly AttachmentId attachment = AttachmentId.New();

    public DownloadAttachmentTests()
    {
        ticket = Ticket.Raise(TicketId.New(), "Subject", "Description", TicketPriority.Low, [new AttachmentFile(attachment, "photo.jpg", "image/jpeg", 4)], DateTimeOffset.UnixEpoch).Value;
        store.Saved.Add(ticket);
    }

    [Fact]
    public async Task An_attachment_is_downloaded_with_its_file_name_and_its_stored_bytes()
    {
        blobs.Blobs[AttachmentBlobs.Name(ticket.Id, attachment)] = [0xFF, 0xD8, 0x00, 0x01];

        var result = await Handle(new DownloadAttachment(ticket.Id.Value, attachment.Value));

        result.IsSuccess.ShouldBeTrue();
        result.Value.FileName.ShouldBe("photo.jpg");
        using var content = new MemoryStream();
        await using (var stream = result.Value.Content)
        {
            await stream.CopyToAsync(content, TestContext.Current.CancellationToken);
        }

        byte[] stored = [0xFF, 0xD8, 0x00, 0x01];
        content.ToArray().ShouldBe(stored);
    }

    [Fact]
    public async Task An_attachment_of_a_ticket_that_is_not_there_is_not_found()
    {
        var result = await Handle(new DownloadAttachment(Guid.CreateVersion7(), attachment.Value));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("tickets.ticket-not-found");
        result.Error.Status.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task An_attachment_the_ticket_does_not_have_is_not_found()
    {
        var result = await Handle(new DownloadAttachment(ticket.Id.Value, Guid.CreateVersion7()));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("tickets.attachment-not-found");
        result.Error.Status.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task An_attachment_whose_file_is_missing_from_storage_is_not_found()
    {
        var result = await Handle(new DownloadAttachment(ticket.Id.Value, attachment.Value));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("tickets.attachment-not-found");
    }

    private Task<Result<AttachmentDownload, ApiError>> Handle(DownloadAttachment query) =>
        new DownloadAttachmentHandler(store, blobs).HandleAsync(query, TestContext.Current.CancellationToken);
}
